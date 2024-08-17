using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Numerics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using MonoGame.Extended.Tweening;
using Vector2 = Microsoft.Xna.Framework.Vector2;
using Vector4 = Microsoft.Xna.Framework.Vector4;

namespace Signalmaster;

public static class UI {
	public static readonly Color colorBackground = new(75, 75, 75);
	public static readonly Color colorSecondary = new(25, 25, 25);

	public static bool InBounds(Vector2 pos, (int x, int y, int w, int h) bounds) {
		float relX = pos.X - bounds.x; // Relative x & y coords
		float relY = pos.Y - bounds.y;
		return (relX >= 0) && (relX <= bounds.w) && (relY >= 0) && (relY <= bounds.h);
	}

	public static bool InBounds((int x, int y) pos, (int x, int y, int w, int h) bounds) {
		return InBounds(new Vector2(pos.x, pos.y), bounds);
	}

	public static bool InBounds(Vector2 pos, Vector2 point1, Vector2 point2) {
		return (pos.X >= Math.Min(point1.X, point2.X)) && (pos.X <= Math.Max(point1.X, point2.X)) && (pos.Y >= Math.Min(point1.Y, point2.Y)) && (pos.Y <= Math.Max(point1.Y, point2.Y));
	}

	public static bool InBounds(Vector2 pos, Vector4 bounds) {
		bounds += new Vector4(-0.1f, 0.1f, -0.1f, 0.1f);
		return pos.X >= bounds.X && pos.X <= bounds.Y && pos.Y >= bounds.Z && pos.Y <= bounds.W;
	}

	public static Rectangle ToRect((int x, int y, int w, int h) bounds) {
		return new Rectangle(bounds.x, bounds.y, bounds.w, bounds.h);
	}

	public static float QuadraticBezierCalc(float[] points, float t) {
		float firstPow = 1-t;
		float secondPow = (1-t)*firstPow;
		return secondPow*points[0] + firstPow*points[1]*2*t + t*t*points[2];
	}

	public static Vector2 QuadraticBezierCalc(Vector2[] points, float t) {
		return new(
			QuadraticBezierCalc(new float[]{points[0].X, points[1].X, points[2].X}, t),
			QuadraticBezierCalc(new float[]{points[0].Y, points[1].Y, points[2].Y}, t)
		);
	}

	public static float CubicBezierCalc(float[] points, float t) {
		float firstPow = 1-t;
		float secondPow = (1-t)*firstPow;
		float thirdPow = (1-t)*secondPow;
		return thirdPow*points[0] + secondPow*points[1]*3*t + firstPow*points[2]*3*t*t + t*t*t*points[3];
	}

	public static Vector2 CubicBezierCalc(Vector2[] points, float t) {
		return new(
			CubicBezierCalc(new float[]{points[0].X, points[1].X, points[2].X, points[3].X}, t),
			CubicBezierCalc(new float[]{points[0].Y, points[1].Y, points[2].Y, points[3].Y}, t)
		);
	}
}

public abstract class UIElement {	
	public abstract void Update(GameTime gameTime);
	public abstract void Draw();
}

public abstract class UIButton : UIElement {
	protected (int x, int y, int w, int h) bounds;
	protected bool pressed, hovered;
	protected readonly Tweener tweener;
	public float tween;
	protected readonly Action action;

	public UIButton(Action _action, (int x, int y) positionBasis, (int x, int y, int w, int h) _bounds, bool boundCentered = false) {
		bounds = _bounds;
		switch(positionBasis.x) {
			case 0:
				break;
			case 1:
				bounds.x = UIManager.GetCenterCoord().x + bounds.x;
				break;
			case 2:
				bounds.x = UIManager.width - (bounds.x + bounds.w);
				break;
		}
		switch(positionBasis.y) {
			case 0:
				break;
			case 1:
				bounds.y = UIManager.GetCenterCoord().y + bounds.y;
				break;
			case 2:
				bounds.y = UIManager.height - (bounds.y + bounds.h);
				break;
		}
		bounds = boundCentered ? (bounds.x - (bounds.w >> 1), bounds.y - (bounds.h >> 1), bounds.w, bounds.h) : bounds;
		tweener = new Tweener();
		tween = 0f;
		pressed = false;
		action = _action;
	}

	public override void Update(GameTime gameTime) {
		Vector2 mousePos = Mouse.GetState().Position.ToVector2();
		bool _hovered = hovered;
		hovered = UI.InBounds(mousePos, bounds);
		pressed = (Mouse.GetState().LeftButton == ButtonState.Pressed) && hovered;
		if(pressed) action();
		tweener.Update(gameTime.GetElapsedSeconds());
		if(_hovered != hovered) {
			if(tween == 0f) {
				tweener.TweenTo(this, player => tween, 2f, 0.25f).Easing(EasingFunctions.SineInOut);
			} else {
				tweener.CancelAll();
				tweener.Dispose();
				tweener.TweenTo(this, player => tween, 0f, tween/4).Easing(EasingFunctions.SineInOut);
			}
		}
	}

	public override void Draw() {
		throw new NotImplementedException();
	}
}

public class UIIconButton : UIButton {
	private readonly Texture2D texPressed, texHovered, texUnpressed;	

	public UIIconButton(Action _action, (string t0, string t1, string t2) textures, (int x, int y) positionBasis, (int x, int y, int w, int h) _bounds, bool boundCentered = false) : base(_action, positionBasis, _bounds, boundCentered) {
		texPressed = UIManager.GetTexture(textures.t0);
		texHovered = UIManager.GetTexture(textures.t1);
		texUnpressed = UIManager.GetTexture(textures.t2); 
	}

	public override void Draw() {
		if(pressed) {UIManager.spriteBatch.Draw(texPressed, UI.ToRect(bounds), Color.White); return;}
		UIManager.spriteBatch.Draw(texUnpressed, UI.ToRect(bounds), Color.White * Math.Max(2f-tween, 0f));
		UIManager.spriteBatch.Draw(texHovered, UI.ToRect(bounds), Color.White * Math.Min(tween, 1f));
	}
}

public class UITextButton : UIButton {
	protected readonly string buttonText;
	protected readonly SpriteFont font;
	protected RenderTarget2D texPressed, texHovered, texUnpressed;	
	protected int padding;

	///<summary>
	///An animated button containing the given text. Must be rendered through a <c>UIManager</c>
	///</summary>
	///<param name="_action">The action to be executed upon the button being clicked.</param>
	///<param name="_buttonText">The text to be displayed within the button.</param>
	///<param name="_font">The <c>SpriteFont</c> to render the text with.</param>
	///<param name="positionBasis">An x-y pair tuple describing how to use the given position. [0: Top/Left, 1: Center, 2:Right/Bottom]</param>
	///<param name="_bounds">The position and size of the button as a 4-int tuple <c>(x, y, w, h)</c></param>
	///<param name="_padding">How much padding, in px, to place around the text.</param>
	///<param name="boundCentered">Whether the position variables are relative to screen coords (false), or relative to the center of the screen (true).</param>
	public UITextButton(Action _action, string _buttonText, SpriteFont _font, (int x, int y) positionBasis, (int x, int y, int w, int h) _bounds, int _padding, bool boundCentered = false) : base(_action, positionBasis, (_bounds.x, _bounds.y, _bounds.w + 22 + 2*_padding, _bounds.h + 22 + 2*_padding), boundCentered) {
		buttonText = _buttonText;
		font = _font;
		padding = _padding;

		GraphicsDevice graphicsDevice = UIManager.graphics.GraphicsDevice;
		SpriteBatch spriteBatch = new(graphicsDevice);

		texPressed = new(graphicsDevice, bounds.w, bounds.h);
		texHovered = new(graphicsDevice, bounds.w, bounds.h);
		texUnpressed = new(graphicsDevice, bounds.w, bounds.h);
		
		// All icon bases are 11x11px images

		graphicsDevice.SetRenderTarget(texUnpressed);
		graphicsDevice.Clear(UI.colorBackground);
		spriteBatch.Begin();
		DrawButtonEdges(spriteBatch); // Can only be "Normal" or "Click" as hover would be a duplicate of normal
		spriteBatch.DrawString(font, buttonText, new Vector2(11 + padding, 11 + padding), Color.White);
		spriteBatch.End();
		graphicsDevice.SetRenderTarget(null);

		graphicsDevice.SetRenderTarget(texHovered);
		graphicsDevice.Clear(UI.colorBackground);
		spriteBatch.Begin();
		DrawButtonEdges(spriteBatch); // Can only be "Normal" or "Click" as hover would be a duplicate of normal
		spriteBatch.FillRectangle(10, 10, bounds.w - 20, bounds.h - 20, Color.White);
		spriteBatch.DrawString(font, buttonText, new Vector2(11 + padding, 11 + padding), UI.colorBackground);
		spriteBatch.End();
		graphicsDevice.SetRenderTarget(null);

		graphicsDevice.SetRenderTarget(texPressed);
		graphicsDevice.Clear(UI.colorBackground);
		spriteBatch.Begin();
		DrawButtonEdges(spriteBatch, 0xFFBFBFBF); // Can only be "Normal" or "Click" as hover would be a duplicate of normal
		spriteBatch.FillRectangle(10, 10, bounds.w - 20, bounds.h - 20, new Color(0xFFBFBFBF));
		spriteBatch.DrawString(font, buttonText, new Vector2(11 + padding, 11 + padding), UI.colorBackground);
		spriteBatch.End();
		graphicsDevice.SetRenderTarget(null);
	}
	public UITextButton(Action _action, string _buttonText, SpriteFont _font, (int x, int y) positionBasis, (int x, int y, int h) _bounds, int padding, bool boundCentered = false) : this(_action, _buttonText, _font, positionBasis, (_bounds.x, _bounds.y, (int)_font.MeasureString(_buttonText).X, _bounds.h), padding, boundCentered) {}
	public UITextButton(Action _action, string _buttonText, SpriteFont _font, (int x, int y) positionBasis, (int x, int y) _bounds, int padding, bool boundCentered = false) : this(_action, _buttonText, _font, positionBasis, (_bounds.x, _bounds.y, (int)_font.MeasureString(_buttonText).X, (int)_font.MeasureString(_buttonText).Y), padding, boundCentered) {}

	public void DrawButtonEdges(SpriteBatch spriteBatch, uint colorInt = 0xFFFFFFFF) {
		Color color = new(colorInt);

		spriteBatch.Draw(UIManager.GetTexture("iconBaseNW"), new Rectangle(0, 0, 11, 11), color);
		spriteBatch.Draw(UIManager.GetTexture("iconBaseNE"), new Rectangle(bounds.w - 11, 0, 11, 11), color);
		spriteBatch.Draw(UIManager.GetTexture("iconBaseSW"), new Rectangle(0, bounds.h - 11, 11, 11), color);
		spriteBatch.Draw(UIManager.GetTexture("iconBaseSE"), new Rectangle(bounds.w - 11, bounds.h - 11, 11, 11), color);

		spriteBatch.Draw(UIManager.GetTexture("iconBaseN"), new Rectangle(11, 0, bounds.w - 22, 11), color);
		spriteBatch.Draw(UIManager.GetTexture("iconBaseE"), new Rectangle(bounds.w - 11, 11, 11, bounds.h - 22), color);
		spriteBatch.Draw(UIManager.GetTexture("iconBaseS"), new Rectangle(11, bounds.h - 11, bounds.w - 22, 11), color);
		spriteBatch.Draw(UIManager.GetTexture("iconBaseW"), new Rectangle(0, 11, 11, bounds.h - 22), color);
	}

	public override void Draw() {
		if(pressed) {UIManager.spriteBatch.Draw(texPressed, UI.ToRect(bounds), Color.White); return;}
		UIManager.spriteBatch.Draw(texUnpressed, UI.ToRect(bounds), Color.White * Math.Max(2f-tween, 0f));
		UIManager.spriteBatch.Draw(texHovered, UI.ToRect(bounds), Color.White * Math.Min(tween, 1f));
	}
}

public class UIToggleTextButton : UITextButton {
	Func<bool> tester;

	public UIToggleTextButton(Func<bool> _tester, Action setter, string _buttonText, SpriteFont _font, (int x, int y) positionBasis, (int x, int y, int w, int h) _bounds, int _padding, bool boundCentered = false) : base(setter, _buttonText, _font, positionBasis, _bounds, _padding, boundCentered) {
		tester = _tester;
	}

	public UIToggleTextButton(Func<bool> _tester, Action setter, string _buttonText, SpriteFont _font, (int x, int y) positionBasis, (int x, int y, int h) _bounds, int _padding, bool boundCentered = false) : base(setter, _buttonText, _font, positionBasis, _bounds, _padding, boundCentered) {
		tester = _tester;
	}

	public UIToggleTextButton(Func<bool> _tester, Action setter, string _buttonText, SpriteFont _font, (int x, int y) positionBasis, (int x, int y) _bounds, int _padding, bool boundCentered = false) : base(setter, _buttonText, _font, positionBasis, _bounds, _padding, boundCentered) {
		tester = _tester;
	}

	public override void Draw() {
		if(tester()) {UIManager.spriteBatch.Draw(texPressed, UI.ToRect(bounds), Color.White); return;}
		UIManager.spriteBatch.Draw(texUnpressed, UI.ToRect(bounds), Color.White * Math.Max(2f-tween, 0f));
		UIManager.spriteBatch.Draw(texHovered, UI.ToRect(bounds), Color.White * Math.Min(tween, 1f));
	}
}

public class UISceneTransition : UIElement {
	private readonly Tweener tweener;
	public float tween;
	private bool transitioned;
	private readonly int w, h;
	private readonly Action transitionAction;

	public UISceneTransition(Action _transitionAction, Action<Tween> endAction) {
		tween = 0f;
		tweener = new Tweener();
		tweener.TweenTo(this, player => tween, 2f, 0.75f).Easing(EasingFunctions.SineInOut).OnEnd(endAction);
		transitioned = false;
		w = UIManager.width;
		h = UIManager.height;
		transitionAction = _transitionAction;
	}

	public override void Update(GameTime gameTime) {
		tweener.Update(gameTime.GetElapsedSeconds());
		if(tween > 1f && !transitioned) {
			transitionAction();
			transitioned = true;
		}
	}

	public override void Draw() {
		UIManager.spriteBatch.FillRectangle(0f, Math.Max(1f - tween, 0f) * h, w, Math.Min(tween, 2f - tween) * h, UI.colorSecondary);
	}
}