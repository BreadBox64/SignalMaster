using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace CreateTask;

public class Game1 : Game
{
	enum Scene {
		Menu,
		Main,
		Settings,
	}

	enum ButtonID {
		MenuPlay,
		MenuLevel,
		MenuEditor,
		MenuSettings,
		MenuExit,
	}

	static bool collide(Vector2 pos, (int, int, int, int) bounds) {
		int relX = (int)pos.X - bounds.Item1; // Relative x & y coords
		int relY = (int)pos.Y - bounds.Item2;
		return (relX >= 0) && (relX <= bounds.Item3) && (relY >= 0) && (relY <= bounds.Item4);
	}

	static readonly Color colorBackground = new Color(50, 50, 50);
	static readonly Color colorDisabled = new Color(100, 100, 100);

	static SpriteFont josefinSans64px;

	private class Button {
		private int x, y, w, h;
		public bool pressed;
		private double animPos, animVel, animMult;
		private int hoverTime;
		private Texture2D pressedTex, unpressedTex, selectionTex;
		private bool textureDefined;

		public Button((int x, int y, int w, int h) coords, double animTime = 1000) {
			(x, y, w, h) = coords;
			pressed = false;
			animPos = 0;
			animVel = 0;
			animMult = 256/animTime;
			hoverTime = 0;
			textureDefined = false;
		}

		public Button((int x, int y, int w, int h) coords, (Texture2D pressed, Texture2D unpressed) tex, double animTime = 5000) {
			(x, y, w, h) = coords;
			pressed = false;
			animPos = 0;
			animVel = 0;
			animMult = 512/animTime;
			hoverTime = 0;
			pressedTex = tex.pressed;
			unpressedTex = tex.unpressed;
			textureDefined = true;
		}

		public void initSelectBoxTexture(GraphicsDevice gd) {
			selectionTex = new Texture2D(gd, w, h);
			Color[] data = new Color[w*h];
			Array.Fill(data, new Color(192, 64, 64, 255));
			selectionTex.SetData(data);
		}

		public void textureLoad((Texture2D pressed, Texture2D unpressed) tex) {
			pressedTex = tex.pressed;
			unpressedTex = tex.unpressed;
			textureDefined = true;
		}

		public void drawSelectionBox(SpriteBatch sb) {
			sb.Draw(selectionTex, new Rectangle(x, y, w, h), new Color(192, 64, 64, 255));
		}

		public void draw(SpriteBatch sb, int frameDelta) {
			if(!textureDefined) return;
			if(animVel == 0) {
				if(pressed) {
					sb.Draw(pressedTex, new Rectangle(x, y, w, h), Color.White);
				} else {
					sb.Draw(unpressedTex, new Rectangle(x, y, w, h), Color.White);
				}
			} else {
				//Console.WriteLine(animPos);
				animPos += animVel * frameDelta;
				if(animPos <= 0) {animPos = 0; animVel = 0;}
				if(animPos >= 511) {animPos = 511; animVel = 0;}
				sb.Draw(pressedTex, new Rectangle(x, y, w, h), new Color(255, 255, 255, (uint)Math.Min(animPos - 255, 0)));
				sb.Draw(unpressedTex, new Rectangle(x, y, w, h), new Color(255, 255, 255, (uint)Math.Min(255 - animPos, 0)));
			}
		}

		public void handleCollision(Vector2 mousePos, (bool left, bool middle, bool right) mouseButtons) {
			if(!mouseButtons.left && animPos == 511) {animVel = -animMult; pressed = false;}
			if(collide(mousePos, (x, y, w, h))) {
				hoverTime += 1;
				if(mouseButtons.left && animPos == 0) {animVel = animMult; pressed = true;}
			} else hoverTime = 0;
		}
	}

	private Dictionary<ButtonID,Button> buttons;
	private Dictionary<ButtonID,(Texture2D, Texture2D)> buttonTextures;

	private Scene currentScene;
	private int _width;
	private int _height;
	private int _frameDelta;
	private int _elapsedTime;
	private MouseState _oldMouseState;
	private GraphicsDeviceManager _graphics;
	private SpriteBatch _spriteBatch;

	public Game1() {
		_graphics = new GraphicsDeviceManager(this);
		Content.RootDirectory = "Content";
		IsMouseVisible = true;
	}

	protected override void Initialize() {
		// Setup window settings
		base.Initialize();
		_width = _graphics.GraphicsDevice.Adapter.CurrentDisplayMode.Width;
		_height = _graphics.GraphicsDevice.Adapter.CurrentDisplayMode.Height;
		_frameDelta = 0;
		_elapsedTime = 0;
		_graphics.PreferredBackBufferWidth = _width;
		_graphics.PreferredBackBufferHeight = _height;
		_graphics.HardwareModeSwitch = false;
		_graphics.ApplyChanges();
		//_graphics.ToggleFullScreen();
		currentScene = Scene.Menu;

		_oldMouseState = Mouse.GetState();
		buttons = new Dictionary<ButtonID, Button>();

		int halfWidth = _width >> 1;
		int halfHeight =  _height >> 1;
		var halfIconSize = (Play: 96, Main: 64, Small:48);

		buttons.Add(ButtonID.MenuPlay, new Button((halfWidth - halfIconSize.Play, halfHeight - halfIconSize.Play - 64, 192, 192), buttonTextures[ButtonID.MenuPlay]));
		buttons.Add(ButtonID.MenuLevel, new Button((halfWidth - halfIconSize.Play + (-4*halfIconSize.Main), halfHeight - halfIconSize.Main - 64, 128, 128), buttonTextures[ButtonID.MenuLevel]));
		buttons.Add(ButtonID.MenuEditor, new Button((halfWidth - halfIconSize.Play + (5*halfIconSize.Main), halfHeight - halfIconSize.Main - 64, 128, 128), buttonTextures[ButtonID.MenuEditor]));
    buttons.Add(ButtonID.MenuSettings, new Button((halfWidth + (int)Math.Round(2.5*halfIconSize.Main) - halfIconSize.Small, halfHeight + (2*halfIconSize.Small), 96, 96), buttonTextures[ButtonID.MenuSettings]));
		buttons.Add(ButtonID.MenuExit, new Button((halfWidth + (int)Math.Round(-2.5*halfIconSize.Main) - halfIconSize.Small, halfHeight + (2*halfIconSize.Small), 96, 96), buttonTextures[ButtonID.MenuExit]));
		foreach(KeyValuePair<ButtonID,Button> kvp in buttons) {
			kvp.Value.initSelectBoxTexture(GraphicsDevice);
		}
	}

	protected override void LoadContent()	{
		_spriteBatch = new SpriteBatch(GraphicsDevice);

		// Load Fonts
		josefinSans64px = Content.Load<SpriteFont>("JS64");

		// Load Images
		buttonTextures = new Dictionary<ButtonID, (Texture2D, Texture2D)>	{
			{ ButtonID.MenuPlay, (Content.Load<Texture2D>("iconFilledPlay"), Content.Load<Texture2D>("iconUnfilledPlay")) },
			{ ButtonID.MenuLevel, (Content.Load<Texture2D>("iconFilledLevel"), Content.Load<Texture2D>("iconUnfilledLevel")) },
			{ ButtonID.MenuEditor, (Content.Load<Texture2D>("iconFilledEditor"), Content.Load<Texture2D>("iconUnfilledEditor")) },
			{ ButtonID.MenuSettings, (Content.Load<Texture2D>("iconFilledSettings"), Content.Load<Texture2D>("iconUnfilledSettings")) },
			{ ButtonID.MenuExit, (Content.Load<Texture2D>("iconFilledExit"), Content.Load<Texture2D>("iconUnfilledExit")) }
		};
	}

	protected override void Update(GameTime gameTime)	{
		if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
			Exit();

		// TODO: Add your update logic here
		MouseState mouseState = Mouse.GetState();
		Vector2 mousePos = mouseState.Position.ToVector2();
		bool leftButtonPressed = mouseState.LeftButton == ButtonState.Pressed && mouseState.LeftButton != _oldMouseState.LeftButton;
		bool middleButtonPressed = mouseState.MiddleButton == ButtonState.Pressed && mouseState.MiddleButton != _oldMouseState.MiddleButton;
		bool rightButtonPressed = mouseState.RightButton == ButtonState.Pressed && mouseState.RightButton != _oldMouseState.RightButton;
		foreach(KeyValuePair<ButtonID,Button> kvp in buttons) {
			kvp.Value.handleCollision(mousePos, (leftButtonPressed, middleButtonPressed, rightButtonPressed));
		}

		base.Update(gameTime);
	}

	private void DrawMenuScene(GameTime gameTime) {
		GraphicsDevice.Clear(colorBackground);
		string title = "SignalMaster";
		_spriteBatch.Begin();
    _spriteBatch.DrawString(josefinSans64px, title, new Vector2((_width - josefinSans64px.MeasureString(title).X) / 2, 192), Color.White);
		foreach(KeyValuePair<ButtonID,Button> kvp in buttons) {
			kvp.Value.drawSelectionBox(_spriteBatch);
			kvp.Value.draw(_spriteBatch, _frameDelta);
		}
    _spriteBatch.End();
	}

	protected override void Draw(GameTime gameTime)	{
		int newElapsed = (int)gameTime.ElapsedGameTime.TotalMilliseconds;
		_frameDelta = newElapsed - _elapsedTime;
		_elapsedTime = newElapsed;

		switch(currentScene) {
			case Scene.Menu:
				DrawMenuScene(gameTime);
				break;
			case Scene.Main:
				break;
			case Scene.Settings:
				break;
			default:
				GraphicsDevice.Clear(Color.Red);
				_spriteBatch.Begin();
				_spriteBatch.DrawString(josefinSans64px, "Uh oh", new Vector2(0, 0), Color.Black);
				_spriteBatch.End();
				break;
		}
		
		base.Draw(gameTime);
	}
}
