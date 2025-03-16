using Godot;
using System;
using System.Collections.Generic;

public partial class Mushroom : Primitive
{
	public Mushroom() : base(Vector2.Zero) {	
		Category = PrimitiveCategory.MovementModifier;
		SetTexture((Texture2D)GD.Load("res://Assets/kenney_platformer-art-deluxe/Mushroom expansion/PNG/tallShroom_red.png")); // Replace with actual path
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
	}
	
	public override bool ValidatePlacement(Room room) {
		//bool hasFloorBelow = room.HasPrimitiveBelow(this.GlobalPosition, typeof(Floor));
//
		//if (!hasFloorBelow) {
			//GD.Print($"❌ ERROR: Mushroom at {this.GlobalPosition} has no valid floor below!");
			//return false;
		//}

		return true;
	}
}
