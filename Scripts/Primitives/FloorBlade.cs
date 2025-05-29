using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class FloorBladeAtom : Atom {
	public FloorBladeAtom() {
		// animated sprite setup
		var animSprite = new AnimatedSprite2D();
		var frames = new SpriteFrames();
		
		frames.AddAnimation("spin");
		frames.SetAnimationSpeed("spin", 10);
		frames.SetAnimationLoop("spin", true);
		
		frames.AddFrame("spin", GD.Load<Texture2D>("res://Assets/kenney_platformer-art-deluxe/Extra animations and enemies/Enemy sprites/spinnerHalf.png"));
		frames.AddFrame("spin", GD.Load<Texture2D>("res://Assets/kenney_platformer-art-deluxe/Extra animations and enemies/Enemy sprites/spinnerHalf_spin.png"));

		animSprite.SpriteFrames = frames;
		animSprite.Play("spin");
		AddChild(animSprite);

		// size + collision shape
		Size = new Vector2(60, 30);
		CollisionShape2D collision = new CollisionShape2D();
		ConvexPolygonShape2D shape = new ConvexPolygonShape2D();

		Vector2[] points = new Vector2[9];
		float radius = 30;

		for (int i = 0; i < 9; i++) {
			float angle = Mathf.DegToRad(180 * i / 8.0f);
			points[i] = new Vector2(Mathf.Cos(angle), -Mathf.Sin(angle)) * radius;
		}

		shape.Points = points;
		collision.Shape = shape;
		collision.Position += new Vector2(0, 15);
		AddChild(collision);

		// group + collision settings
		AddToGroup("FloorBlade");
		SetCollisionLayerValue(6, true);
		SetCollisionMaskValue(1, true);
	}
	
	public override bool ValidatePlacement(Room room) {
		// always valid
		return true;
	}
}

public partial class FloorBlade : Primitive {

	public FloorBlade() : base(Vector2.Zero) {
		Category = PrimitiveCategory.Hazard;
		Difficulty = 2;
	}

	public FloorBlade(Vector2 position) : base(position) {}

	public override bool GenerateInRoom(Room room) {
		// check for empty space around blade
		if (room.HasAtomAt(this.Position)
		|| room.HasAtomAt(this.Position - new Vector2(0, 70))
		|| room.HasAtomAt(this.Position + new Vector2(70, 0))
		|| room.HasAtomAt(this.Position - new Vector2(70, 0))
		|| room.HasAtomOfTypeAt(this.Position - new Vector2(70, 70), typeof(FullBladeAtom))
		|| room.HasAtomOfTypeAt(this.Position - new Vector2(-70, 70), typeof(FullBladeAtom))
		|| !room.HasAtomOfTypeAt(this.Position + new Vector2(70, 70), typeof(FloorTile)) 
		|| !room.HasAtomOfTypeAt(this.Position + new Vector2(-70, 70), typeof(FloorTile))) {
			return false;
		}

		// spawn blade
		FloorBladeAtom atom = new FloorBladeAtom();
		atom.GlobalPosition = this.Position;
		AddAtom(atom);

		return room.AddPrimitive(this);
	}

	public override void GenerateAnchors(Room room) {
		// adds one anchor just above blade
		Anchors.Clear();
		List<Atom> atoms = GetAtoms();
		Atom atom = atoms.First();
		
		Vector2 topPosition = atom.GlobalPosition - new Vector2(0, atom.Size.Y / 2 + 5);
		float orbit = 40f;

		Anchor topAnchor = new Anchor(topPosition, orbit, "over_blade", this);
		Anchors.Add(topAnchor);
		
		GenerateObstructionLines();
	}

	public void GenerateObstructionLines() {
		// collision line across top of blade
		ObstructionLines.Clear();

		List<Atom> atoms = GetAtoms();
		Atom atom = atoms.First();
		
		Vector2 bottomLeft = atom.GlobalPosition + new Vector2((-atom.Size.X / 2) - 10, (atom.Size.Y / 2) - 2);
		Vector2 bottomRight = atom.GlobalPosition + new Vector2((atom.Size.X / 2) + 10, (atom.Size.Y / 2) - 2);

		ObstructionLines.Add((bottomLeft, bottomRight));
	}
}
