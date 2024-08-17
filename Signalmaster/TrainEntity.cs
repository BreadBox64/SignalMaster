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
			gameManager.trackMap.OccupyTrack(spawnTrack.ID, 2);
		}
		finalTrackID = spawn.destinationTrackID;
	}

	public int NumCars() {
		return trainCars.Length;
	}

	private void ChangeTrack(Track newTrack, TrainBogey bogey, float overrideDistance = 0) {
		gameManager.trackMap.UnoccupyTrack(bogey.currentTrack.ID);
		gameManager.trackMap.OccupyTrack(newTrack.ID);
		bogey.currentTrack = newTrack;
		bogey.currentTrackShape = newTrack.trackShape;
		Vector2[] points = bogey.currentTrack.points;
		switch(bogey.currentTrackShape) {
			case TrackShape.Linear:
				points = dirForward? points : new Vector2[]{points[1], points[0]};
				bogey.rotation = (points[1] - points[0]).ToAngle() - (float)(Math.PI/2); // 0 is (0, -1) pi/2 is (1, 0)
				bogey.velocity = new Vector2(speed, 0).Rotate(bogey.rotation);
				bogey.position += new Vector2(overrideDistance, 0).Rotate(bogey.rotation);
				bogey.currentTrackBounds = new(Math.Min(points[0].X, points[1].X), Math.Max(points[0].X, points[1].X), Math.Min(points[0].Y, points[1].Y), Math.Max(points[0].Y, points[1].Y));
			break;
			case TrackShape.Arc:

			break;
		}

		// NOTE FOR FUTURE: put in a constant t offset approach and see how well it works
	}
 
	private void Update(float deltaTime, TrainBogey bogey) {
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
		if(trainCars.Last().rear.currentTrack.ID == finalTrackID) {
			gameManager.trackMap.UnoccupyTrack(finalTrackID, (ushort)(2*trainCars.Length));
			gameManager.RemoveTrain(this);
		}
	}

	public override void Draw() {
		foreach(var (fore, rear) in trainCars) {
			UIManager.spriteBatch.Draw(texture, fore.position, null, Color.White, (fore.position - rear.position).ToAngle() + (float)(Math.PI/2), textureRotationCenter, 1.0f, SpriteEffects.None, 1.0f);
		}
	}
}
