using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
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
		foreach(string mapFileName in System.IO.Directory.GetFiles("maps")) {
			try {
				string[] mapContent = System.IO.File.ReadAllLines(mapFileName);
				if(mapContent[0] == "<INFO>" && mapContent.Contains("<TRACK>") && mapContent.Contains("<SPAWN>") && mapContent.Contains("<END>")) {
					mapStrings.Add(mapFileName[5..^4], mapContent);
				} else {
					Console.WriteLine($"<ERROR> Map file '{mapFileName}' was invalid");
				}
			}
			catch(System.IO.FileNotFoundException) {
				Console.WriteLine($"<ERROR> Map file '{mapFileName}' could not be found");
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

class TrainBogey {
	public Vector2 position, velocity;
	public float rotation;
	public Track currentTrack;
	public TrackShape currentTrackShape;
	public Vector4 currentTrackBounds;

	public TrainBogey(Vector2 _position, Vector2 _velocity, float _rotation, Track _currentTrack, TrackShape _currentTrackShape, Vector4? _currentTrackBounds = null) {
		position = _position;
		velocity = _velocity;
		rotation = _rotation;
		currentTrack = _currentTrack;
		currentTrackShape = _currentTrackShape;
		currentTrackBounds = _currentTrackBounds ?? Vector4.Zero;
	}
}

class TrainEntity : Entity {
	(TrainBogey fore, TrainBogey rear)[] trainCars;
	float speed;
	bool dirForward;
	Texture2D texture;
	Vector2 textureRotationCenter;
	StationStop[] destinations;
	int finalTrackID;
	GameManager gameManager;

	public TrainEntity(GameManager gm, Spawn spawn) {
		texture = UIManager.GetTexture("trainCar");
		textureRotationCenter = new(0, texture.Height/2);
		gameManager = gm;
		destinations = spawn.stations;
		speed = spawn.trainSpeed;
		dirForward = spawn.dirForward;
		Track spawnTrack = gm.trackMap.tracks[spawn.spawnTrackID - 1];
		trainCars = new (TrainBogey fore, TrainBogey rear)[spawn.numCars];
		for(int i = 0; i < spawn.numCars; i++) {
			trainCars[i].fore = new(spawnTrack.points[dirForward? 1 : 0], Vector2.Zero, 0, spawnTrack, spawnTrack.trackShape);
			ChangeTrack(spawnTrack, trainCars[i].fore, i*(-40)-4);
			trainCars[i].rear = new(spawnTrack.points[dirForward? 1 : 0], Vector2.Zero, 0, spawnTrack, spawnTrack.trackShape);
			ChangeTrack(spawnTrack, trainCars[i].rear, (i*(-40))-36);
		}
		finalTrackID = spawn.destinationTrackID;
	}

	public int NumCars() {
		return trainCars.Length;
	}

	public void ChangeTrack(Track newTrack, TrainBogey bogey, float overrideDistance = 0) {
		bogey.currentTrack = newTrack;
		bogey.currentTrackShape = newTrack.trackShape;
		Vector2[] points = bogey.currentTrack.points;
		if(bogey.currentTrackShape == TrackShape.Linear) {
			points = dirForward? points : new Vector2[]{points[1], points[0]};
			bogey.rotation = (points[1] - points[0]).ToAngle() - (float)(Math.PI/2); // 0 is (0, -1) pi/2 is (1, 0)
			bogey.velocity = new Vector2(speed, 0).Rotate(bogey.rotation);
			bogey.position += new Vector2(overrideDistance, 0).Rotate(bogey.rotation);
			bogey.currentTrackBounds = new(Math.Min(points[0].X, points[1].X), Math.Max(points[0].X, points[1].X), Math.Min(points[0].Y, points[1].Y), Math.Max(points[0].Y, points[1].Y));
		}
	}
 
	public void Update(float deltaTime, TrainBogey bogey) {
		bogey.position += bogey.velocity * deltaTime;
		switch(bogey.currentTrackShape) {
			case TrackShape.Linear:
				if(!UI.InBounds(bogey.position, bogey.currentTrackBounds)) {
					float overrideDistance = Vector2.Distance(bogey.position, bogey.currentTrack.points[dirForward? 1 : 0]);
					int newTrackID = dirForward? bogey.currentTrack.foreConnection : bogey.currentTrack.rearConnection;
					Debug.Assert(newTrackID != 0); // If trackID ever == 0 something broke horribly
					Track newTrack = gameManager.trackMap.tracks[newTrackID - 1];
					bogey.position = bogey.currentTrack.points[dirForward? 1 : 0];
					ChangeTrack(newTrack, bogey, overrideDistance);
				}
				break;
		}
	}

	public override void Update(float deltaTime) {
		foreach(var (fore, rear) in trainCars) {
			Update(deltaTime, fore);
			Update(deltaTime, rear);
		}
		if(trainCars.Last().rear.currentTrack.ID == finalTrackID) gameManager.RemoveTrain(this);
	}

	public override void Draw() {
		foreach(var (fore, rear) in trainCars) {
			UIManager.spriteBatch.Draw(texture, fore.position, null, Color.White, (fore.position - rear.position).ToAngle() + (float)(Math.PI/2), textureRotationCenter, 1.0f, SpriteEffects.None, 1.0f);
		}
	}
}

class SignalEntity : Entity {
	public SignalEntity() {

	}

	public override void Update(float gameTimeSeconds) {
		
	}

	public override void Draw() {
		
	}
}

enum TrackShape {
	Linear,
	QuadraticBezier,
	CubicBezier,
}

enum TrainType {
	PassengerNormal,
	PassengerExpress,
	PassengerMilitary,
}

struct Station {
	public int[] forwardTrackIDs;
	public int[] rearwardTrackIDs;
	public string stationName;

	public Station(string _stationName, int[] _forwardTrackIDs, int[] _rearwardTrackIDs) {
		stationName = _stationName;
		forwardTrackIDs = _forwardTrackIDs;
		rearwardTrackIDs = _rearwardTrackIDs;
	}
}

struct StationStop {
	public Station station;
	public bool dirForward;
	public int[] validTracks;
	public float stopTime;

	public StationStop(Station _station, bool _dirForward, int[] _validTracks, float _stopTime) {
		station = _station;
		dirForward = _dirForward;
		validTracks = _validTracks;
		stopTime = _stopTime;
	}
}

struct Spawn {
	public int time;
	public int spawnTrackID, destinationTrackID;
	public TrainType trainType;
	public int numCars;
	public float trainSpeed;
	public bool dirForward;
	public bool containsStations;
	public StationStop[] stations;

	public Spawn(int _time, int _spawnTrackID, int _destinationTrackID, TrainType _trainType, int _numCars, float _trainSpeed, bool _dirForward, string[] _stations, TrackMap parent) {
		time = _time;
		spawnTrackID = _spawnTrackID;
		destinationTrackID = _destinationTrackID;
		trainType = _trainType;
		numCars = _numCars;
		trainSpeed = _trainSpeed;
		dirForward = _dirForward;
		containsStations = _stations != null && _stations.Length != 0;
		if(containsStations) {
			stations = new StationStop[_stations.Length];
			for(int i = 0; i < stations.Length; i++) {
				string[] stationStopData = _stations[i].Split('-');
				Station station = parent.stations[stationStopData[0]];
				int[] stationIDs = dirForward? station.forwardTrackIDs : station.rearwardTrackIDs;
				stations[i] = new(
					station,
					dirForward,
					stationStopData[2].Split(',').Select(x => stationIDs[int.Parse(x)]).ToArray(),
					float.Parse(stationStopData[1])
				);
			}
		} else stations = null;
	}
}

class Track {
	public readonly int ID;
	public readonly TrackShape trackShape;
	public readonly Vector2[] points;
	public int foreConnection;
	public int rearConnection;
	public bool switchTrack;

	public Track(int _ID, TrackShape _trackShape, Vector2[] _points, int _foreConnection, int _rearConnection, bool _switchTrack) {
		ID = _ID;
		trackShape = _trackShape;
		points = _points;
		foreConnection = _foreConnection;
		rearConnection = _rearConnection;
		switchTrack = _switchTrack;
	}
}

class SwitchEntity : Entity {
	int origin;
	int[] destinations;
	int destinationIndex;
	Vector2 switchPos;
	bool dirForward;
	TrackMap parent;

	public SwitchEntity(int _origin, int[] _destinations, Vector2 _switchPos, bool _dirForward, TrackMap _parent) {
		origin = _origin;
		destinations = _destinations;
		switchPos = _switchPos;
		dirForward = _dirForward;
		parent = _parent;
		destinationIndex = 0;
	}

	public void Update(float gameTimeSeconds, bool click) {
		if(click && Vector2.Distance(Mouse.GetState().Position.ToVector2(), switchPos) <= 25) {
			destinationIndex++;
			if(destinationIndex == destinations.Length) destinationIndex = 0;
			if(dirForward) {
				parent.tracks[origin - 1].foreConnection = destinations[destinationIndex];
			} else {
				parent.tracks[origin - 1].rearConnection = destinations[destinationIndex];
			}
			parent.mapChanged = true;
		}
	}

	public void Draw(SpriteBatch spriteBatch) {
		spriteBatch.DrawLine(parent.tracks[origin-1].points[0], parent.tracks[origin-1].points[1], Color.Black, 4.0f);
		spriteBatch.DrawLine(parent.tracks[destinations[destinationIndex]-1].points[0], parent.tracks[destinations[destinationIndex]-1].points[1], Color.Black, 4.0f);
	}
}

class TrackMap {
	string mapName;
	public Dictionary<string, int> trackIDRef;
	public Track[] tracks;
	public SwitchEntity[] switches;
	public Dictionary<string, Station> stations;
	GraphicsDeviceManager graphics;
	RenderTarget2D trackMapTexture;
	public bool mapChanged;

	public TrackMap(GraphicsDeviceManager _graphics, string[] mapContent, Queue<Spawn> spawnQueue) {
		graphics = _graphics;
		trackMapTexture = new(
			graphics.GraphicsDevice,
			graphics.PreferredBackBufferWidth,
			graphics.PreferredBackBufferHeight,
			false,
			SurfaceFormat.Color,
			DepthFormat.Depth24,
			8,
			RenderTargetUsage.DiscardContents
		);
		mapChanged = false;

		mapName = mapContent[1];
		int trackIndex = Array.IndexOf(mapContent, "<TRACK>");
		int switchIndex = Array.IndexOf(mapContent, "<SWITCH>");
		int signalIndex = Array.IndexOf(mapContent, "<SIGNAL>");
		int stationIndex = Array.IndexOf(mapContent, "<STATION>");
		int spawnIndex = Array.IndexOf(mapContent, "<SPAWN>");
		int endIndex = Array.IndexOf(mapContent, "<END>");
		float xScaleFactor = UIManager.width / 1600f;
		float yScaleFactor = UIManager.height / 900f;

		trackIDRef = new() {
			{ "#", 0 }
		};
		for(int i = trackIndex + 1; i < switchIndex; i++) {
			string id = mapContent[i][..mapContent[i].IndexOf('|')];
			trackIDRef.Add(id, trackIDRef.Count);
		}
		tracks = new Track[trackIDRef.Count - 1];
		for(int i = trackIndex + 1; i < switchIndex; i++) {
			string[] options = mapContent[i].Split('|');
			Vector2[] points = new Vector2[options.Length - 4];
			for(int j = 3; j < options.Length - 1; j++) {
				string[] coords = options[j].Split(',');
				points[j-3] = new Vector2(float.Parse(coords[0])*xScaleFactor, float.Parse(coords[1])*yScaleFactor);
			}
			
			tracks[i-(trackIndex+1)] = new(
				trackIDRef[options[0]],
				(TrackShape)int.Parse(options[1]),
				points,
				trackIDRef[options[^1]], 
				trackIDRef[options[2]],
				options[0][0] == '&'
			);
		}

		switches = new SwitchEntity[signalIndex - (switchIndex+1)];
		for(int i = switchIndex + 1; i < signalIndex; i++) {
			string[] options = mapContent[i].Split('|');
			string[] coords = options[0].Split(',');
			switches[i-(switchIndex+1)] = new(
				trackIDRef[options[3]],
				options[4..].Select(x => trackIDRef[x]).ToArray(),
				new Vector2(float.Parse(coords[0])*xScaleFactor, float.Parse(coords[1])*yScaleFactor),
				bool.Parse(options[2]),
				this
			);
		}

		for(int i = signalIndex + 1; i < stationIndex; i++) {}

		stations = new Dictionary<string, Station>(spawnIndex - (stationIndex+1));
		for(int i = stationIndex + 1; i < spawnIndex; i++) {
			string[] options = mapContent[i].Split('|');
			stations.Add(options[0], new(
				options[0],
				options[1].Split(',').Select(x => trackIDRef[x]).ToArray(),
				options[2].Split(',').Select(x => trackIDRef[x]).ToArray()
			));
		} 

		spawnQueue.Clear();
		for(int i = spawnIndex + 1; i < endIndex; i++) {
			string[] options = mapContent[i].Split('|');
			int timeHours = int.Parse(options[0][0..2]);
			int timeMins = int.Parse(options[0][2..4]);
			int timeTotal = 60*timeHours + timeMins;                                               // Each real-life second is an in-game minute
			Console.WriteLine($"<INFO>: Spawn Gen (Ln{i}) - {timeTotal}");
			spawnQueue.Enqueue(new(
				timeTotal,
				trackIDRef[options[5]],
				trackIDRef[options[^1]],
				(TrainType)int.Parse(options[2]),
				int.Parse(options[3]),
				float.Parse(options[4]),
				bool.Parse(options[1]),
				(options.Length == 7)? null : options[6..^1],
				this
			));
		}

		PreDraw();
	}

	public void Update() {
		bool click = MouseExtended.GetState().WasButtonJustDown(MouseButton.Left);
		foreach(SwitchEntity switchEntity in switches) switchEntity.Update(0, click);
		if(mapChanged) {
			PreDraw();
			mapChanged = false;
		}
	}

	public void PreDraw() {
		GraphicsDevice graphicsDevice = UIManager.graphics.GraphicsDevice;
		graphicsDevice.SetRenderTarget(trackMapTexture);
		graphicsDevice.Clear(UI.colorBackground);
		SpriteBatch spriteBatch = new(graphicsDevice);
		spriteBatch.Begin();

		foreach(Track track in tracks) {
			Color drawColor = track.switchTrack? Color.DarkGray : Color.LightGray;
			if(track.trackShape == TrackShape.Linear) spriteBatch.DrawLine(track.points[0], track.points[1], drawColor, 4.0f); else {
				Vector2 previousPoint = track.points[0];
				Vector2 currentPoint = track.points[^1];
				bool cubicBezier = track.trackShape == TrackShape.CubicBezier;
				int segments = (int)Math.Floor(0.1*Vector2.Distance(previousPoint, currentPoint));
				for(float i = 1; i <= segments; i++) {
					currentPoint = cubicBezier? UI.CubicBezierCalc(track.points, i/segments) : UI.QuadraticBezierCalc(track.points, i/segments);
					spriteBatch.DrawLine(previousPoint, currentPoint, drawColor, 4.0f);
					previousPoint = currentPoint;
				}
			}
		}

		foreach(SwitchEntity switchEntity in switches) switchEntity.Draw(spriteBatch);

		spriteBatch.End();
		graphicsDevice.SetRenderTarget(null);
	}

	public void Draw() {
		UIManager.spriteBatch.Draw(trackMapTexture, new Vector2(0, 0), Color.White);
	}
}