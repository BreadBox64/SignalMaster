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
	Arc,
}

class Track {
	public readonly int ID;
	public readonly TrackShape trackShape;
	public readonly Vector2[] points;
	public int foreConnection;
	public int rearConnection;
	public bool switchTrack;
	public bool enabled;

	public Track(int _ID, TrackShape _trackShape, Vector2[] _points, int _foreConnection, int _rearConnection, bool _switchTrack) {
		ID = _ID;
		trackShape = _trackShape;
		points = _points;
		foreConnection = _foreConnection;
		rearConnection = _rearConnection;
		switchTrack = _switchTrack;
		enabled = !_switchTrack;
	}
}

struct SwitchState {
	public (int trackID, bool enabled)[] twoPartRules;
	public (int trackID, int foreConnection, bool enabled)[] threePartForeRules;
	public (int trackID, int rearConnection, bool enabled)[] threePartRearRules;
	public (int trackID, int foreConnection, int rearConnection, bool enabled)[] fourPartRules;

	public SwitchState((int, bool)[] tPR, (int, int, bool)[] tPFR, (int, int, bool)[] tPRR, (int, int, int, bool)[] fPR) {
		twoPartRules = tPR;
		threePartForeRules = tPFR;
		threePartRearRules = tPRR;
		fourPartRules = fPR;
	}
}

class SwitchEntity : Entity {
	private Rectangle switchBounds;
	private int[] tracks;
	private int stateIndex;
	private int maxStateIndex;
	private SwitchState[] states;
	private TrackMap parent;

	public SwitchEntity(Rectangle _switchBounds, int numSwitchStates, int initialState, int[] _tracks, TrackMap _parent) {
		switchBounds = _switchBounds;
		tracks = _tracks;
		parent = _parent;
		stateIndex = initialState;
		maxStateIndex = numSwitchStates - 1;
		states = new SwitchState[numSwitchStates];
	}

	private bool Locked() {
		foreach(int id in tracks) {
			if(parent.IsTrackOccupied(id)) return true;
		}
		return false;
	}

	public void SetState(int index, SwitchState state) {
		states[index] = state;
	}

	public void Update(float gameTimeSeconds, bool click, Vector2 mousePos) {
		if(click && switchBounds.Contains(mousePos) && !Locked()) {
			stateIndex = (stateIndex == maxStateIndex) ? 0 : stateIndex + 1;
			ExecuteState();
			parent.MapUpdate();
		}
	}

	public void Draw(SpriteBatch spriteBatch) {

	}

	public void ForceExecuteState() {
		ExecuteState();
	}

	private void ExecuteState() {
		SwitchState state = states[stateIndex];
		foreach((int trackID, bool enabled) rule in state.twoPartRules) {
			parent.tracks[rule.trackID-1].enabled = rule.enabled;
		}
		foreach((int trackID, int foreConnection, bool enabled) rule in state.threePartForeRules) {
			parent.tracks[rule.trackID-1].enabled = rule.enabled;
			parent.tracks[rule.trackID-1].foreConnection = rule.foreConnection;
		}
		foreach((int trackID, int rearConnection, bool enabled) rule in state.threePartRearRules) {
			parent.tracks[rule.trackID-1].enabled = rule.enabled;
			parent.tracks[rule.trackID-1].rearConnection = rule.rearConnection;
		}
		foreach((int trackID, int rearConnection, int foreConnection, bool enabled) rule in state.fourPartRules) {
			parent.tracks[rule.trackID-1].enabled = rule.enabled;
			parent.tracks[rule.trackID-1].foreConnection = rule.foreConnection;
			parent.tracks[rule.trackID-1].rearConnection = rule.rearConnection;
		}
	}
}

enum TrainType {
	PassengerNormal,
	PassengerExpress,
	PassengerMilitary,
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
