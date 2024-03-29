﻿using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;

namespace Signalmaster;

public class Game1 : Game {
	public enum Scene {
		Menu,
		Main,
		Settings,
	}

	static readonly Color colorBackground = new Color(50, 50, 50);
	static readonly Color colorDisabled = new Color(100, 100, 100);

	static SpriteFont JS64;

	public Scene currentScene;
	private int _width;
	private int _height;
	private double _frameDelta;
	private double _elapsedTime;
	private MouseState _oldMouseState;
	private GraphicsDeviceManager _graphics;
	private SpriteBatch _spriteBatch;
	private UIManager _UIManager;

	public Game1() {
		_graphics = new GraphicsDeviceManager(this);
		_UIManager = new UIManager(this, _graphics);
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
		UIManager.SetScreenSize(_width, _height);
		ChangeScene(Scene.Menu);

		_oldMouseState = Mouse.GetState();
	}

	protected override void LoadContent()	{
		_spriteBatch = new SpriteBatch(GraphicsDevice);
		UIManager.Init(_spriteBatch, Content);

		// Load Fonts
		UIManager.LoadSpriteFonts(new string[] {"JS64"});
		UIManager.LoadTextures(new string[] {"iconPlayClick", "iconPlayHover", "iconPlayNormal"});
		JS64 = UIManager.GetFont("JS64");
	}

	protected override void Update(GameTime gameTime)	{
		if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
			Exit();

		_UIManager.Update(gameTime);
		base.Update(gameTime);
	}

	private void ChangeScene(Scene newScene) {
		switch(newScene) {
			case Scene.Menu:
				_UIManager.AddUIElement(new UIIconButton(("iconPlayClick", "iconPlayHover", "iconPlayNormal"), (0, 0, 192, 192), true));
				break;
			case Scene.Main:
				break;
			case Scene.Settings:
				break;
		}
	}

	private void DrawMenuScene(GameTime gameTime) {
		GraphicsDevice.Clear(colorBackground);
		string title = "SignalMaster";
		_spriteBatch.Begin();
    _spriteBatch.DrawString(JS64, title, new Vector2((_width - JS64.MeasureString(title).X) / 2, 192), Color.White);
		_UIManager.Draw();
    _spriteBatch.End();
	}

	protected override void Draw(GameTime gameTime)	{
		double newElapsed = (int)gameTime.ElapsedGameTime.TotalMilliseconds;
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
				_spriteBatch.DrawString(JS64, "Uh oh", new Vector2(0, 0), Color.Black);
				_spriteBatch.End();
				break;
		}
		
		base.Draw(gameTime);
	}
}
