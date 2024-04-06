using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;

namespace Signalmaster;

class GameManager {
	public List<TrainEntity> trains;
	public List<SignalEntity> signals;
	public TrackMap trackMap;

	public GameManager() {
		trains = new List<TrainEntity>();
		signals = new List<SignalEntity>();
	}

	public void LoadMap(ContentManager contentManager, string mapFileName) {
		string mapContent = contentManager.Load<string>(mapFileName);
		Console.WriteLine(mapContent);
		trackMap = new TrackMap("");
	}

	public void Update(GameTime gameTime) {
		foreach(TrainEntity train in trains) train.Update(gameTime);
	}
	
	public void Draw() {
		trackMap.Draw();
		foreach(TrainEntity train in trains) train.Draw();
		foreach(SignalEntity signal in signals) signal.Draw();
	}
}

class Entity {
	public virtual void Update(GameTime gameTime) {
		throw new NotImplementedException();
	}

	public virtual void Draw() {
		throw new NotImplementedException();
	}
}

class TrainEntity : Entity {
	public TrainEntity() {
		
	}

	public override void Update(GameTime gameTime) {
		
	}

	public override void Draw() {
		
	}
}

class SignalEntity : Entity {
	public SignalEntity() {

	}

	public override void Update(GameTime gameTime) {
		
	}

	public override void Draw() {
		
	}
}

class SwitchEntity : Entity {
	public SwitchEntity() {

	}

	public override void Update(GameTime gameTime) {
		
	}

	public override void Draw() {
		
	}
}

class TrackMap {
	SwitchEntity[] swtiches;

	public TrackMap(string mapContent) {

	}

	public void Draw() {

	}
}