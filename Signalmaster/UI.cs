using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using MonoGame.Extended.Content;
using MonoGame.Extended.Sprites;
using MonoGame.Extended.Animations;
using MonoGame.Extended.Input;
using MonoGame.Extended.Tweening;
using System.Linq;
using MonoGame.Extended.Collections;

namespace Signalmaster;

public static class UI {
	public static readonly Color colorBackground = new(50, 50, 50);
	public static readonly Color colorSecondary = new(125, 125, 125);

	public static bool InBounds(Vector2 pos, (int x, int y, int w, int h) bounds) {
		int relX = (int)pos.X - bounds.x; // Relative x & y coords
		int relY = (int)pos.Y - bounds.y;
		return (relX >= 0) && (relX <= bounds.w) && (relY >= 0) && (relY <= bounds.h);
	}

	public static bool InBounds((int x, int y) pos, (int x, int y, int w, int h) bounds) {
		int relX = pos.x - bounds.x; // Relative x & y coords
		int relY = pos.y - bounds.y;
		return (relX >= 0) && (relX <= bounds.w) && (relY >= 0) && (relY <= bounds.h);
	}

	public static Rectangle ToRect((int x, int y, int w, int h) bounds) {
		return new Rectangle(bounds.x, bounds.y, bounds.w, bounds.h);
	}

	public class MouseArea {

	}
}

public class UIElement {	
	public virtual void Update(GameTime gameTime) {
		throw new NotImplementedException();
	}

	public virtual void Draw() {
		throw new NotImplementedException();
	}
}

public class UIIconButton : UIElement {
	private readonly Texture2D texPressed, texHovered, texUnpressed;
	private (int x, int y, int w, int h) bounds;
	private bool pressed, hovered;
	private readonly Tweener tweener;
	public float tween;
	private readonly Action action;

	public UIIconButton(Action _action, (string t0, string t1, string t2) textures, (int x, int y, int w, int h) _bounds, bool boundCentered = false, bool positionCentered = false) {
		texPressed = UIManager.GetTexture(textures.t0);
		texHovered = UIManager.GetTexture(textures.t1);
		texUnpressed = UIManager.GetTexture(textures.t2);
		bounds = positionCentered ? (UIManager.GetCenterCoord().x + _bounds.x, UIManager.GetCenterCoord().y + _bounds.y, _bounds.w, _bounds.h) : _bounds;
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
		if(pressed) {UIManager.spriteBatch.Draw(texPressed, UI.ToRect(bounds), Color.White); return;}
		UIManager.spriteBatch.Draw(texUnpressed, UI.ToRect(bounds), Color.White * Math.Max(2f-tween, 0f));
		UIManager.spriteBatch.Draw(texHovered, UI.ToRect(bounds), Color.White * Math.Min(tween, 1f));
	}
}

public class UIAnimatedButton : UIElement {
	public UIAnimatedButton() {
		
	}
}

public class UISceneTransition : UIElement {
	private readonly Game1.Scene scene;
	private readonly Tweener tweener;
	public float tween;
	private bool transitioned;
	private readonly int w, h;
	private readonly Action transitonAction;

	public UISceneTransition(Game1.Scene _scene, Action _transitonAction, Action<Tween> endAction) {
		scene = _scene;
		tween = 0f;
		tweener = new Tweener();
		tweener.TweenTo(this, player => tween, 2f, 0.75f).Easing(EasingFunctions.SineInOut).OnEnd(endAction);
		transitioned = false;
		w = UIManager.width;
		h = UIManager.height;
		transitonAction = _transitonAction;
	}

	public override void Update(GameTime gameTime) {
		tweener.Update(gameTime.GetElapsedSeconds());
		if(tween > 1f && !transitioned) {
			transitonAction();
			transitioned = true;
		}
	}

	public override void Draw() {
		UIManager.spriteBatch.FillRectangle(0f, Math.Max(1f - tween, 0f) * h, w, Math.Min(tween, 2f - tween) * h, UI.colorSecondary);
	}
}

public class UIManager {
	private static Game1 game;
	private static GraphicsDeviceManager graphics;
	public static int width, height;
	public static SpriteBatch spriteBatch;
	private static ContentManager contentManager;
	public static Dictionary<string, SpriteFont> fonts;
	public static Dictionary<string, Texture2D> textures;
	private static Texture2D nullTexture;
	private List<UIElement> UIElements;
	private List<Action> preUpdateActions;
	private UISceneTransition sceneTransition;

	public UIManager(Game1 game1, GraphicsDeviceManager graphics1) {
		game = game1;
		graphics = graphics1;
		fonts = new Dictionary<string, SpriteFont>();
		textures = new Dictionary<string, Texture2D>();
		UIElements = new List<UIElement>();
		preUpdateActions = new List<Action>();
	}

	public static void Init(SpriteBatch sb, ContentManager cm) {
		spriteBatch = sb;
		contentManager = cm;
		nullTexture = contentManager.Load<Texture2D>("nullTexture");
	}

	public static void SetScreenSize(int w, int h) {
		(width, height) = (w, h);
	}

	// Content Loading Methods

	public static void LoadSpriteFonts(string[] fontNames) {
		foreach(string fontName in fontNames) {
			fonts.Add(fontName, contentManager.Load<SpriteFont>(fontName));
		}
	}

	public static void LoadTextures(string[] textureNames) {
		foreach(string textureName in textureNames) {
			textures.Add(textureName, contentManager.Load<Texture2D>(textureName));
		}
	}

	// Getter/Setter Methods

	public static SpriteFont GetFont(string fontName) {
		return fonts[fontName];
	}

	public static Texture2D GetTexture(string textureName) {
		try	{
			return textures[textureName];
		}	catch (KeyNotFoundException) {
			Console.WriteLine($"<WARNING> Texture {textureName} is null, using default.");
			return nullTexture;
		}
	}

	public static (int x, int y) GetCenterCoord() {
		return (width >> 1, height >> 1);
	}

	// Usage Methods

	public void AddUIElement(UIElement element) {
		UIElements.Add(element);
	}

	public void ClearUIElements() {
		UIElements.Clear();
	}

	public Action AddSceneTransition(Game1.Scene scene) {
		return () => {
			sceneTransition = new(scene, () => {
				AddPreUpdateActions(new Action[] {
					ClearUIElements,
					() => {
						game.ChangeScene(scene);
					}
				});
			}, (tween) => {
				sceneTransition = null;
			});
		};
	}

	public void AddPreUpdateAction(Action action) {
		preUpdateActions.Add(action);
	}

	public void AddPreUpdateActions(Action[] actions) {
		foreach(Action action in actions) {
			preUpdateActions.Add(action);
		}
	}

	public void Update(GameTime gameTime) {
		if(preUpdateActions.Count != 0) {
			foreach(Action action in preUpdateActions) {
				action();
			}
			preUpdateActions.Clear();
		}
		foreach(UIElement element in UIElements) {
			element.Update(gameTime);
		}
		sceneTransition?.Update(gameTime);
	}
	
	public void Draw() {
		foreach(UIElement element in UIElements) {
			element.Draw();
		}
		sceneTransition?.Draw();
	}
}