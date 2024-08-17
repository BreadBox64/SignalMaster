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

class TrackMap {
	public Dictionary<string, int> trackIDRef;
	public Track[] tracks;
	private int[] trackOccupations;
	public SwitchEntity[] switches;
	public Dictionary<string, Station> stations;
	GraphicsDeviceManager graphics;
	RenderTarget2D trackMapTexture;
	private bool mapChanged;
	public void MapUpdate() {mapChanged = true;}

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

		mapContent = mapContent.Where(line => !line.StartsWith("//")).ToArray();
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
			try {
			string[] options = mapContent[i].Split('|');
			Debug.Assert(options.Length >= 6); // Minimum Length for a fully valid track definition
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
			catch (Exception) {
				// All thrown line numbers are based off of the uncommented lines only
				Console.WriteLine($"<ERROR> Map build failed during track construction at line {i+1}, aborting map load!");
				throw;
			}
		}

		trackOccupations = new int[tracks.Length];

		List<SwitchEntity> switchList = new();
		int stateIndex = 0;
		for(int i = switchIndex + 1; i < signalIndex; i++) {
			if(mapContent[i][0] == '#') {
				string[] options = mapContent[i].Split('|');
				Rectangle bounds;
				string[] coords = options[1].Split(',');
				if(options.Length > 5) { // Did the spec provide a size for the bounding box?
					string[] size = options[2].Split(',');
					bounds = new((int)(float.Parse(coords[0])*xScaleFactor) - 25, (int)(float.Parse(coords[1])*yScaleFactor) - 25, (int)(float.Parse(size[0])*xScaleFactor) + 50, (int)(float.Parse(size[1])*yScaleFactor) + 50);
					switchList.Add(new SwitchEntity(bounds, int.Parse(options[3]), int.Parse(options[4]), options[5].Split(',').Select(x => trackIDRef[x]).ToArray(), this));
				} else {
					bounds = new((int)(float.Parse(coords[0])*xScaleFactor) - 25, (int)(float.Parse(coords[1])*yScaleFactor) - 25, 50, 50);
					switchList.Add(new SwitchEntity(bounds, int.Parse(options[2]), int.Parse(options[3]), options[4].Split(',').Select(x => trackIDRef[x]).ToArray(), this));
				}
				stateIndex = 0;
			} else {
				List<(int, bool)> tPR = new();
				List<(int, int, bool)> tPFR = new();
				List<(int, int, bool)> tPRR = new();
				List<(int, int, int, bool)> fPR = new();
				string[] rules = mapContent[i].Split(',');
				foreach(string rule in rules) {
					string[] options = rule.Split('|');
					int trackID = trackIDRef[options[0]];
					bool enabled = bool.Parse(options[1]);
					if(options.Length == 2) tPR.Add((trackID, enabled)); else {
						if(options[2] == "*") {
							tPFR.Add((trackID, trackIDRef[options[3]], enabled));
						} else if(options[3] == "*") {
							tPRR.Add((trackID, trackIDRef[options[2]], enabled));
						} else {
							fPR.Add((trackID, trackIDRef[options[2]], trackIDRef[options[3]], enabled));
						}
					}
				}
				switchList.Last().SetState(stateIndex, new SwitchState(tPR.ToArray(), tPFR.ToArray(), tPRR.ToArray(), fPR.ToArray()));
				stateIndex++;
			}
		}
		switches = switchList.ToArray();
		foreach(SwitchEntity switchEntity in switches) switchEntity.ForceExecuteState();

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
			int timeTotal = 60*timeHours + timeMins; // Each real-life second is an in-game minute
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

	public void OccupyTrack(int id, ushort count = 1) {
		id -= 1;
		if(trackOccupations[id] == 0) MapUpdate();
		trackOccupations[id] += count;
	}

	public void UnoccupyTrack(int id, ushort count = 1) {
		id -= 1;
		trackOccupations[id] -= count;
		if(trackOccupations[id] == 0) MapUpdate();
	}

	public bool IsTrackOccupied(int id) {
		return trackOccupations[id - 1] != 0;
	}

	public void Update() {
		MouseStateExtended mouseState = MouseExtended.GetState();
		bool click = mouseState.WasButtonJustDown(MouseButton.Left);
		foreach(SwitchEntity switchEntity in switches) switchEntity.Update(0, click, mouseState.Position.ToVector2());
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
			Color drawColor = track.enabled? IsTrackOccupied(track.ID)? Color.DarkRed : Color.LightGray : Color.Black;
			switch(track.trackShape) {
				case TrackShape.Linear:
					spriteBatch.DrawLine(track.points[0], track.points[1], drawColor, 4.0f);
				break;
				case TrackShape.QuadraticBezier:
				case TrackShape.CubicBezier:
					Vector2 previousPoint = track.points[0];
					Vector2 currentPoint = track.points[^1];
					bool cubicBezier = track.trackShape == TrackShape.CubicBezier;
					int segments = (int)Math.Floor(0.1*Vector2.Distance(previousPoint, currentPoint));
					for(float i = 1; i <= segments; i++) {
						currentPoint = cubicBezier? UI.CubicBezierCalc(track.points, i/segments) : UI.QuadraticBezierCalc(track.points, i/segments);
						spriteBatch.DrawLine(previousPoint, currentPoint, drawColor, 4.0f);
						previousPoint = currentPoint;
					}
				break;
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