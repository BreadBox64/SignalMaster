using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Signalmaster;

public class Game1 : Game {
	public enum Scene {
		Menu,
		Main,
		Settings,
		Exit,
	}

	static SpriteFont JS64;

	public Scene currentScene;
	private int _width;
	private int _height;
	private GraphicsDeviceManager _graphics;
	private SpriteBatch _spriteBatch;
	private UIManager _UIManager;
	private GameManager _GameManager;
	private Action backAction;

	public Game1() {
		_graphics = new GraphicsDeviceManager(this);
		_UIManager = new UIManager(this, _graphics);
		_GameManager = new GameManager();
		Content.RootDirectory = "Content";
		IsMouseVisible = true;
		backAction = () => {};
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
		_graphics.ToggleFullScreen();
		currentScene = Scene.Menu;
		UIManager.SetScreenSize(_width, _height);
		ChangeScene(Scene.Menu);
	}

	protected override void LoadContent()	{
		_spriteBatch = new SpriteBatch(GraphicsDevice);
		UIManager.Init(_spriteBatch, Content);

		// Load Fonts
		UIManager.LoadSpriteFonts(new string[] {"JS64"});
		UIManager.LoadTextures(new string[] {"iconPlayClick", "iconPlayHover", "iconPlayNormal"});
		UIManager.LoadTextures(new string[] {"iconExitClick", "iconExitHover", "iconExitNormal"});
		UIManager.LoadTextures(new string[] {"iconSettingsClick", "iconSettingsHover", "iconSettingsNormal"});
		JS64 = UIManager.GetFont("JS64");
	}

	protected override void Update(GameTime gameTime)	{
		if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
			backAction();

		_UIManager.Update(gameTime);
		base.Update(gameTime);
	}

	public void ChangeScene(Scene newScene) {
		switch(newScene) {
			case Scene.Menu:
				_UIManager.AddUIElement(new UIIconButton(_UIManager.AddSceneTransition(Scene.Main), ("iconPlayClick", "iconPlayHover", "iconPlayNormal"), (0, 0, 192, 192), (1, 1), true));
				_UIManager.AddUIElement(new UIIconButton(_UIManager.AddSceneTransition(Scene.Exit), ("iconExitClick", "iconExitHover", "iconExitNormal"), (256, 0, 128, 128), (1, 1), true));
				_UIManager.AddUIElement(new UIIconButton(_UIManager.AddSceneTransition(Scene.Settings), ("iconSettingsClick", "iconSettingsHover", "iconSettingsNormal"), (-256, 0, 128, 128), (1, 1), true));
				backAction = _UIManager.AddSceneTransition(Scene.Exit);
				break;
			case Scene.Main:
				backAction = _UIManager.AddSceneTransition(Scene.Menu);
				break;
			case Scene.Settings:
				_UIManager.AddUIElement(new UIIconButton(_UIManager.AddSceneTransition(Scene.Menu), ("iconExitClick", "iconExitHover", "iconExitNormal"), (8, 8, 64, 64), (2, 0)));
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
			case Scene.Menu:
				GraphicsDevice.Clear(UI.colorBackground);
				_spriteBatch.DrawString(JS64, "SignalMaster", new Vector2((_width - JS64.MeasureString("SignalMaster").X) / 2, 192), Color.White);
				break;
			case Scene.Main:
				_GameManager.Draw();
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