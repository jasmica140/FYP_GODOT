using Godot;
using System;
using System.Collections.Generic;

public partial class FloorBladeAtom : Atom {
	public FloorBladeAtom() {
		SetTexture((Texture2D)GD.Load("res://Assets/kenney_platformer-art-deluxe/Extra animations and enemies/Enemy sprites/spinnerHalf.png")); 
		
		CollisionShape2D collision = new CollisionShape2D();
		ConvexPolygonShape2D shape = new ConvexPolygonShape2D();
		Vector2[] points = new Vector2[9];
		float radius = 30;

		for (int i = 0; i < 9; i++) {
			float angle = Mathf.DegToRad(180 * i / 8.0f); // 0° to 180°
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
	
	public override void GenerateInRoom(Room room) {
		List<Vector2> validPositions = room.GetPositionsAboveFloorTiles();

		if (validPositions.Count == 0) {
			GD.Print($"⚠️ WARNING: No valid floor tile positions found for {this.GetType().Name}");
			return;
		}

		// Pick a random valid position from the list
		Random random = new Random();
		Vector2 chosenPosition = validPositions[random.Next(validPositions.Count)];

		FloorBladeAtom atom = new FloorBladeAtom();
		atom.GlobalPosition = chosenPosition + new Vector2(0, 20);
		AddAtom(atom);
		room.AddAtom(atom); 
		
		this.Position = atom.GlobalPosition;
		room.AddPrimitive(this);
	}
}
