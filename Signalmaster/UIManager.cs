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

public class UIManager {
	private static Game1 game;
	public static GraphicsDeviceManager graphics;
	public static int width, height;
	public static SpriteBatch spriteBatch;
	private static ContentManager contentManager;
	public static Dictionary<string, SpriteFont> fonts;
	public static Dictionary<string, Texture2D> textures;
	public static Texture2D nullTexture;
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
			if(!Game1.sceneTransitionActive) {
				Game1.sceneTransitionActive = true;
				sceneTransition = new(() => {
					AddPreUpdateActions(new Action[] {
						ClearUIElements,
						() => {
							game.ChangeScene(scene);
						}
					});
				}, (tween) => {
					sceneTransition = null;
					Game1.sceneTransitionActive = false;
				});
			}
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