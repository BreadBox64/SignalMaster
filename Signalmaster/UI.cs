using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using MonoGame.Extended.Sprites;

namespace Signalmaster;

public static class UI {
	static readonly Color colorBackground = new Color(50, 50, 50);
	static readonly Color colorDisabled = new Color(100, 100, 100);

	static bool collide(Vector2 pos, (int, int, int, int) bounds) {
		int relX = (int)pos.X - bounds.Item1; // Relative x & y coords
		int relY = (int)pos.Y - bounds.Item2;
		return (relX >= 0) && (relX <= bounds.Item3) && (relY >= 0) && (relY <= bounds.Item4);
	}
}

public class UIManager {
	private Game1 game;
	private GraphicsDeviceManager graphics;
	private int width, height;
	private SpriteBatch spriteBatch;
	private ContentManager contentManager;
	private Dictionary<string, SpriteFont> fonts;

	public UIManager(Game1 game1, GraphicsDeviceManager graphics1) {
		game = game1;
		graphics = graphics1;
		fonts = new Dictionary<string, SpriteFont>();
	}

	public void init(int w, int h, SpriteBatch sb, ContentManager cm) {
		(width, height) = (w, h);
		spriteBatch = sb;
		contentManager = cm;
	}

	public SpriteFont loadSpriteFont(string fontName) {
		SpriteFont font = contentManager.Load<SpriteFont>(fontName);
		fonts.Add(fontName, font);
		return font;
	}

	public void Update() {

	}
	
	public void Draw() {
		
	}
}