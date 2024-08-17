using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using MonoGame.Extended.Input;
using Vector2 = Microsoft.Xna.Framework.Vector2;
using Vector4 = Microsoft.Xna.Framework.Vector4;

namespace Signalmaster;

class GameManager {
	public List<TrainEntity> trains;
	public List<TrainEntity> trainRemovals;
	public List<SignalEntity> signals;
	public TrackMap trackMap;
	public readonly GraphicsDeviceManager graphics;
	private Dictionary<string, string[]> mapStrings;
	private Queue<Spawn> spawns;
	private float gameTimeSeconds;
	private float gameSpeed;
	public float GameSpeed {get {return gameSpeed;}	set {gameSpeed = value;}}

	public GameManager(GraphicsDeviceManager _graphics) {
		trains = new();
		trainRemovals = new();
		signals = new();
		graphics = _graphics;
		mapStrings = new();
		spawns = new();
		gameTimeSeconds = 0;
		gameSpeed = 1;
	}

	public void LoadMapFiles() {
		string mapDirectory = Game1.devMode ? Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..") + "\\maps") : Game1.appDataDirectory + "\\maps";
		foreach(string mapFileName in Directory.GetFiles(mapDirectory, "*.smm")) {
			Console.WriteLine($"<INFO> Loading map '{mapFileName}'.");
			try {
				string[] mapContent = File.ReadAllLines(mapFileName);
				if(mapContent[0] == "t") {
					if(mapContent[1] == "<INFO>" && mapContent.Contains("<TRACK>") && mapContent.Contains("<SWITCH>") && mapContent.Contains("<SIGNAL>") && mapContent.Contains("<STATION>") && mapContent.Contains("<SPAWN>") && mapContent[^1] == "<END>") {
						mapStrings.Add(mapContent[2], mapContent);
					} else {
						Console.WriteLine($"<ERROR> Map file '{mapFileName}' failed to load: invalid data sectioning.");
					}
				} else {
					Console.WriteLine($"<ERROR> Map file '{mapFileName}' failed to load: unsupported file format.");
				}
			}
			catch(FileNotFoundException) {
				Console.WriteLine($"<ERROR> Map file '{mapFileName}' failed to load: file not found.");
			}
			catch(ArgumentException) {
				Console.WriteLine($"<ERROR> Map file '{mapFileName}' failed to load: duplicate map name.");
			}
		}
	}

	public void ReloadMapFiles() {
		mapStrings.Clear();
		LoadMapFiles();
		trackMap.PreDraw();
	}

	public void SetMap(string mapName) {
		trackMap = new TrackMap(graphics, mapStrings[mapName], spawns);
	}

	public bool ValidSpawn() {
		return spawns.Count != 0 && (spawns.Peek().time <= gameTimeSeconds);
	}

	public void RemoveTrain(TrainEntity train) {
		Game1.score += train.NumCars();
		trainRemovals.Add(train);
		Console.WriteLine("<INFO>: Removed Train");
	}

	public void Update(GameTime gameTime) {
		float deltaTime = gameSpeed * gameTime.GetElapsedSeconds();
		while(ValidSpawn()) trains.Add(new(this, spawns.Dequeue()));
		foreach(TrainEntity train in trains) train.Update(deltaTime);
		trackMap.Update();
		foreach(TrainEntity train in trainRemovals) trains.Remove(train);
		trainRemovals.Clear();
		gameTimeSeconds += deltaTime;
	}
	
	public void Draw() {
		trackMap.Draw();
		foreach(TrainEntity train in trains) train.Draw();
		foreach(SignalEntity signal in signals) signal.Draw();
	}
}

class Entity {
	public virtual void Update(float gameTimeSeconds) {
		throw new NotImplementedException();
	}

	public virtual void Draw() {
		throw new NotImplementedException();
	}
}