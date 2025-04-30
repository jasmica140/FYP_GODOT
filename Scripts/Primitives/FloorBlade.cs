using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class FloorBladeAtom : Atom {
	public FloorBladeAtom() {
		// Create animated sprite
		var animSprite = new AnimatedSprite2D();
		var frames = new SpriteFrames();
		
		frames.AddAnimation("spin");
		frames.SetAnimationSpeed("spin", 10); // 4 FPS
		frames.SetAnimationLoop("spin", true);
		
		frames.AddFrame("spin", GD.Load<Texture2D>("res://Assets/kenney_platformer-art-deluxe/Extra animations and enemies/Enemy sprites/spinnerHalf.png"));
		frames.AddFrame("spin", GD.Load<Texture2D>("res://Assets/kenney_platformer-art-deluxe/Extra animations and enemies/Enemy sprites/spinnerHalf_spin.png"));

		animSprite.SpriteFrames = frames;
		animSprite.Play("spin");

		AddChild(animSprite);

		Size = new Vector2(60, 30);

		// Collision shape (unchanged)
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

		AddToGroup("FloorBlade");

		SetCollisionLayerValue(4, true);
		SetCollisionMaskValue(1, true);
	}
	
	public override bool ValidatePlacement(Room room) {
		// Ensure Mushroom is placed on a floor
		return true;
	}
}

public partial class FloorBlade : Primitive
{
	public FloorBlade() : base(Vector2.Zero) {	
		Category = PrimitiveCategory.Hazard;
	}  // Default constructor needed for instantiation
	
	public FloorBlade(Vector2 position) : base(position) {}
	
	
	public override bool GenerateInRoom(Room room) {
		FloorBladeAtom atom = new FloorBladeAtom();
		atom.GlobalPosition = this.Position;
		AddAtom(atom);
		
		return room.AddPrimitive(this);
	}
	
	//public override bool GenerateInRoom(Room room) {
		//List<Vector2> validPositions = room.GetPositionsAboveFloorTiles();
//
		//if (validPositions.Count == 0) {
			//GD.Print($"⚠️ WARNING: No valid floor tile positions found for {this.GetType().Name}");
			//return false;
		//}
//
		//// Pick a random valid position from the list
		//Random random = new Random();
		//Vector2 chosenPosition = validPositions[random.Next(validPositions.Count)];
//
		//FloorBladeAtom atom = new FloorBladeAtom();
		//atom.GlobalPosition = chosenPosition + new Vector2(0, 20);
		//AddAtom(atom);
		//room.AddAtom(atom); 
		//
		//this.Position = chosenPosition;
		//return room.AddPrimitive(this);
	//}
	
	public override void GenerateAnchors(Room room)
	{
		Anchors.Clear();

		List<Atom> atoms = GetAtoms(); // This should return the ladder tiles
		Atom atom = atoms.First();
		
		Vector2 topPosition = atom.GlobalPosition - new Vector2(0, atom.Size.Y / 2 + 5); // 10 pixels above the top
		float orbit = 40f; // orbit radius around the anchor

		Anchor topAnchor = new Anchor(topPosition, orbit, "over_blade");
		Anchors.Add(topAnchor);
		
		GenerateObstructionLines();
	}
	
	public void GenerateObstructionLines()
	{
		ObstructionLines.Clear();
		
		List<Atom> atoms = GetAtoms(); // This should return the ladder tiles
		Atom atom = atoms.First();
		
		Vector2 bottomLeft = atom.GlobalPosition + new Vector2((-atom.Size.X / 2) - 10, (atom.Size.Y / 2) - 2);
		Vector2 bottomRight = atom.GlobalPosition + new Vector2((atom.Size.X / 2) + 10, (atom.Size.Y / 2) - 2);

		ObstructionLines.Add((bottomLeft, bottomRight));
	}
}
