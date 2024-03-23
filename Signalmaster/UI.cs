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
	static readonly Color colorBackground = new(50, 50, 50);
	static readonly Color colorDisabled = new(100, 100, 100);

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
	private Texture2D texPressed, texHovered, texUnpressed;
	private (int x, int y, int w, int h) bounds;
	private bool pressed, hovered;
	private Tweener tweener;
	public float tween;


	public UIIconButton((string t0, string t1, string t2) textures, (int x, int y, int w, int h) _bounds, bool boundCentered = false) {
		texPressed = UIManager.GetTexture(textures.t0);
		texHovered = UIManager.GetTexture(textures.t1);
		texUnpressed = UIManager.GetTexture(textures.t2);
		if(!boundCentered) {
			bounds = _bounds;
		} else {
			(int x, int y) center = UIManager.GetCenterCoord();
			(int w, int h) halfSize = (_bounds.w >> 1, _bounds.h >> 1);
			bounds = (center.x - halfSize.w, center.y - halfSize.h, _bounds.w, _bounds.h);
		}
		tweener = new Tweener();
		tween = 0f;
		pressed = false;
	}

	public override void Update(GameTime gameTime) {
		Vector2 mousePos = Mouse.GetState().Position.ToVector2();
		bool _hovered = hovered;
		hovered = UI.InBounds(mousePos, bounds);
		pressed = Mouse.GetState().LeftButton == ButtonState.Pressed;
		tweener.Update(gameTime.GetElapsedSeconds());
		if(_hovered != hovered) {
			if(tween == 0f) {
				tweener.TweenTo(this, player => tween, 1f, 0.5f).Easing(EasingFunctions.SineInOut);
			} else {
				tweener.CancelAll();
				tweener.Dispose();
				tweener.TweenTo(this, player => tween, 0f, tween/2).Easing(EasingFunctions.SineInOut);
			}
		}
	}

	public override void Draw() {
		if(pressed) {UIManager.spriteBatch.Draw(texPressed, UI.ToRect(bounds), Color.White); return;}
		UIManager.spriteBatch.Draw(texUnpressed, UI.ToRect(bounds), Color.White * (1-tween));
		UIManager.spriteBatch.Draw(texHovered, UI.ToRect(bounds), Color.White * tween);
	}
}

public class UIAnimatedButton : UIElement {
	public UIAnimatedButton() {
		
	}
}

public class UIManager {
	private static Game1 game;
	private static GraphicsDeviceManager graphics;
	private static int width, height;
	public static SpriteBatch spriteBatch;
	private static ContentManager contentManager;
	public static Dictionary<string, SpriteFont> fonts;
	public static Dictionary<string, Texture2D> textures;
	private static Texture2D nullTexture;
	private List<UIElement> UIElements;

	public UIManager(Game1 game1, GraphicsDeviceManager graphics1) {
		game = game1;
		graphics = graphics1;
		fonts = new Dictionary<string, SpriteFont>();
		textures = new Dictionary<string, Texture2D>();
		UIElements = new List<UIElement>();
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

	public void Update(GameTime gameTime) {
		foreach(UIElement element in UIElements) {
			element.Update(gameTime);
		}
	}
	
	public void Draw() {
		foreach(UIElement element in UIElements) {
			element.Draw();
		}
	}
}