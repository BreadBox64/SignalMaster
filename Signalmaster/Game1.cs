using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;

namespace Signalmaster;

public class Game1 : Game {
	public enum Scene {
		Startup,
		Menu,
		Main,
		Settings,
		Exit,
	}

	public Scene currentScene;
	private int _width;
	private int _height;
	private GraphicsDeviceManager _graphics;
	private SpriteBatch _spriteBatch;
	private UIManager _UIManager;
	private GameManager _GameManager;

	public static string appDataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\BreadBox Interactive\\Signalmaster";
	public static readonly bool devMode = true;
	private Action backAction;
	public static bool sceneTransitionActive;
	public static int score;
	public static SpriteFont JS64, JS24;

	public Game1() {
		_graphics = new GraphicsDeviceManager(this);
		_UIManager = new UIManager(this, _graphics);
		_GameManager = new GameManager(_graphics);
		Content.RootDirectory = "Content";
		IsMouseVisible = true;
		sceneTransitionActive = false;
		backAction = () => {};
		score = 0;
	}

	protected override void Initialize() {
		// Setup window settings
		base.Initialize();
		_width = _graphics.GraphicsDevice.Adapter.CurrentDisplayMode.Width;
		_height = _graphics.GraphicsDevice.Adapter.CurrentDisplayMode.Height;
		_graphics.PreferredBackBufferWidth = _width;
		_graphics.PreferredBackBufferHeight = _height;
		_graphics.HardwareModeSwitch = false;
		_graphics.ApplyChanges();
		if(!System.Diagnostics.Debugger.IsAttached) _graphics.ToggleFullScreen();
		currentScene = Scene.Startup;
		UIManager.SetScreenSize(_width, _height);
		ChangeScene(Scene.Startup);
		_GameManager.SetMap("Map0");
	}

	protected override void LoadContent()	{
		_spriteBatch = new SpriteBatch(GraphicsDevice);
		UIManager.Init(_spriteBatch, Content);

		// Load Fonts
		UIManager.LoadSpriteFonts(new string[] {"JS64", "JS24"});
		string[] dirs = {"N", "NE", "E", "SE", "S", "SW", "W", "NW"};
		UIManager.LoadTextures(dirs.Select(e => "iconBase" + e).ToArray());
		UIManager.LoadTextures(new string[] {"trainCar"});
		UIManager.LoadTextures(new string[] {"iconPlayClick", "iconPlayHover", "iconPlayNormal"});
		UIManager.LoadTextures(new string[] {"iconExitClick", "iconExitHover", "iconExitNormal"});
		UIManager.LoadTextures(new string[] {"iconSettingsClick", "iconSettingsHover", "iconSettingsNormal"});
		JS64 = UIManager.GetFont("JS64");
		JS24 = UIManager.GetFont("JS24");

		_GameManager.LoadMapFiles();
	}

	protected override void Update(GameTime gameTime)	{
		if(GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))	backAction();
		if(currentScene == Scene.Main) _GameManager.Update(gameTime);
		_UIManager.Update(gameTime);
		base.Update(gameTime);
	}

	public void ChangeScene(Scene newScene) {
		switch(newScene) {
			case Scene.Startup:
				_UIManager.AddSceneTransition(Scene.Menu)();
			break;
			case Scene.Menu:
				_UIManager.AddUIElement(new UIIconButton(_UIManager.AddSceneTransition(Scene.Main), ("iconPlayClick", "iconPlayHover", "iconPlayNormal"), (1, 1), (0, 0, 192, 192), true));
				_UIManager.AddUIElement(new UIIconButton(_UIManager.AddSceneTransition(Scene.Exit), ("iconExitClick", "iconExitHover", "iconExitNormal"), (1, 1),  (256, 0, 128, 128), true));
				_UIManager.AddUIElement(new UIIconButton(_UIManager.AddSceneTransition(Scene.Settings), ("iconSettingsClick", "iconSettingsHover", "iconSettingsNormal"), (1, 1), (-256, 0, 128, 128), true));
				backAction = _UIManager.AddSceneTransition(Scene.Exit);
			break;
			case Scene.Main:
				_UIManager.AddUIElement(new UIToggleTextButton(() => {return _GameManager.GameSpeed == 0.5f;}, () => {_GameManager.GameSpeed = 0.5f;}, "Slow", JS24, (2, 0), (16, 16, 24), 8));
				_UIManager.AddUIElement(new UIToggleTextButton(() => {return _GameManager.GameSpeed == 1.0f;}, () => {_GameManager.GameSpeed = 1.0f;}, "Normal", JS24, (2, 0), (16, 96, 24), 8));
				_UIManager.AddUIElement(new UIToggleTextButton(() => {return _GameManager.GameSpeed == 2.0f;}, () => {_GameManager.GameSpeed = 2.0f;}, "Fast", JS24, (2, 0), (16, 176, 24), 8));
				backAction = _UIManager.AddSceneTransition(Scene.Menu);
			break;
			case Scene.Settings:
				_UIManager.AddUIElement(new UIIconButton(_UIManager.AddSceneTransition(Scene.Menu), ("iconExitClick", "iconExitHover", "iconExitNormal"), (2, 0), (16, 16, 96, 96)));
				_UIManager.AddUIElement(new UITextButton(_GameManager.ReloadMapFiles, "Reload Maps", JS24, (0, 0), (16, 16, 24), 8));
				backAction = _UIManager.AddSceneTransition(Scene.Menu);
			break;
			case Scene.Exit:
				Exit();
			break;
		}
		currentScene = newScene;
	}

	protected override void Draw(GameTime gameTime)	{
		GraphicsDevice.Clear(UI.colorBackground);
		_spriteBatch.Begin();
		switch(currentScene) {
			case Scene.Startup:
			 GraphicsDevice.Clear(UI.colorSecondary);
			break;
			case Scene.Menu:
				_spriteBatch.DrawString(JS64, "SignalMaster", new Vector2((_width - JS64.MeasureString("SignalMaster").X) / 2, 192), Color.White);
			break;
			case Scene.Main:
				_GameManager.Draw();
				_spriteBatch.DrawString(JS24, $"Score: {score}", new Vector2(8, _height - 36), Color.White);
			break;
			case Scene.Settings:
			break;
			default:
				_spriteBatch.DrawString(JS64, "Uh oh", new Vector2(0, 0), Color.White);	
			break;
		}
		_UIManager.Draw();
		_spriteBatch.End();

		base.Draw(gameTime);
	}
}