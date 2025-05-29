using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
public partial class FullBladeAtom : Atom {
	public FullBladeAtom() {
		// animated sprite setup
		var animSprite = new AnimatedSprite2D();
		var frames = new SpriteFrames();
		
		frames.AddAnimation("spin");
		frames.SetAnimationSpeed("spin", 10);
		frames.SetAnimationLoop("spin", true);
		
		frames.AddFrame("spin", GD.Load<Texture2D>("res://Assets/kenney_platformer-art-deluxe/Extra animations and enemies/Enemy sprites/spinner.png"));
		frames.AddFrame("spin", GD.Load<Texture2D>("res://Assets/kenney_platformer-art-deluxe/Extra animations and enemies/Enemy sprites/spinner_spin.png"));

		animSprite.SpriteFrames = frames;
		animSprite.Play("spin");
		AddChild(animSprite);

		// size + circular collision
		Size = new Vector2(70, 70);
		CollisionShape2D collision = new CollisionShape2D();
		CircleShape2D shape = new CircleShape2D();
		shape.Radius = 30;
		collision.Shape = shape;
		AddChild(collision);

		// group + collision settings
		AddToGroup("FullBlade");
		SetCollisionLayerValue(6, true);
		SetCollisionMaskValue(1, true);
	}
	
	public override bool ValidatePlacement(Room room) {
		// always valid
		return true;
	}
}

public partial class FullBlade : Primitive {

	public FullBlade() : base(Vector2.Zero) {
		Category = PrimitiveCategory.Hazard;
		Difficulty = 2;
	}

	public FullBlade(Vector2 position) : base(position) {}

	public override bool GenerateInRoom(Room room) {
		// check for space around and under blade
		if (room.HasAtomAt(this.Position) || room.HasAtomAt(this.Position + new Vector2(0, 20))
		|| room.HasAtomAt(this.Position + new Vector2(70, 0)) || room.HasAtomAt(this.Position + new Vector2(-70, 0))
		|| room.HasAtomAt(this.Position + new Vector2(70, 20)) || room.HasAtomAt(this.Position + new Vector2(-70, 20))
		|| !room.HasAtomOfTypeAt(this.Position + new Vector2(70, 70), typeof(FloorTile))
		|| !room.HasAtomOfTypeAt(this.Position + new Vector2(-70, 70), typeof(FloorTile))) 
		{ 
			return false;
		}

		// move blade up by one tile
		this.Position -= new Vector2(0, 70);

		// spawn atom
		FullBladeAtom atom = new FullBladeAtom();
		atom.GlobalPosition = this.Position;
		AddAtom(atom);

		return room.AddPrimitive(this);
	}

	public override void GenerateAnchors(Room room) {
		// anchor below blade (player passes under)
		Anchors.Clear();

		List<Atom> atoms = GetAtoms();
		Atom atom = atoms.First();

		Vector2 topPosition = atom.GlobalPosition + new Vector2(0, 105);
		float orbit = 40f;

		Anchor topAnchor = new Anchor(topPosition, orbit, "underblade", this);
		Anchors.Add(topAnchor);

		GenerateObstructionLines();
	}

	public void GenerateObstructionLines() {
		// draws square around blade + horizontal line where floor should be
		ObstructionLines.Clear();

		List<Atom> atoms = GetAtoms();
		Atom atom = atoms.First();

		Vector2 topLeft = atom.GlobalPosition + new Vector2((-atom.Size.X / 2), -(atom.Size.Y / 2));
		Vector2 topRight = atom.GlobalPosition + new Vector2((atom.Size.X / 2), -(atom.Size.Y / 2));
		Vector2 bottomLeft = atom.GlobalPosition + new Vector2((-atom.Size.X / 2), (atom.Size.Y / 2));
		Vector2 bottomRight = atom.GlobalPosition + new Vector2((atom.Size.X / 2), (atom.Size.Y / 2));

		Vector2 floorLeft = atom.GlobalPosition + new Vector2((-atom.Size.X / 2), (atom.Size.Y / 2) + 68);
		Vector2 floorRight = atom.GlobalPosition + new Vector2((atom.Size.X / 2), (atom.Size.Y / 2) + 68);

		ObstructionLines.Add((topLeft, topRight));
		ObstructionLines.Add((topLeft, bottomLeft));
		ObstructionLines.Add((topRight, bottomRight));
		ObstructionLines.Add((bottomLeft, bottomRight));
		ObstructionLines.Add((floorLeft, floorRight));
	}
}
