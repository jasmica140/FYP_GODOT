using Godot;
using System;
using System.Collections.Generic;

public partial class MushroomAtom : Atom {
	public MushroomAtom() {
		SetTexture((Texture2D)GD.Load("res://Assets/kenney_platformer-art-deluxe/Mushroom expansion/PNG/tallShroom_red.png")); 
		
		CollisionShape2D collision = new CollisionShape2D();
		CapsuleShape2D shape = new CapsuleShape2D();
		shape.Radius = 6;
		shape.Height = 38; 
		collision.Shape = shape;
		collision.RotationDegrees = 90;
		collision.Position = new Vector2(-1, 1);

		AddChild(collision);
		AddToGroup("Mushroom");
		
		SetCollisionLayerValue(2, true);
		SetCollisionMaskValue(1, true);
	}
	
	public override bool ValidatePlacement(Room room) {
		// Ensure Mushroom is placed on a floor
		return room.HasPrimitiveBelow(GlobalPosition, typeof(Floor));
	}
}

public partial class Mushroom : Primitive
{
	public Mushroom() : base(Vector2.Zero) {	
		Category = PrimitiveCategory.Test;
	}  // Default constructor needed for instantiation
	
	public Mushroom(Vector2 position) : base(position) {}
	
	public override void GenerateInRoom(Room room) {
		List<Vector2> validPositions = room.GetPositionsAboveFloorTiles();

		if (validPositions.Count == 0) {
			GD.Print($"⚠️ WARNING: No valid floor tile positions found for {this.GetType().Name}");
			return;
		}

		// Pick a random valid position from the list
		Random random = new Random();
		Vector2 chosenPosition = validPositions[random.Next(validPositions.Count)];
		
		this.GlobalPosition = chosenPosition;
		room.AddPrimitive(this);

		MushroomAtom atom = new MushroomAtom();
		atoms.Add(atom);
		AddChild(atom);
		atom.GlobalPosition = chosenPosition;
		room.AddAtom(atom); // ✅ `AddAtom()` is called here to place each FloorTile atom
	}
}
