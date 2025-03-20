using Godot;
using System;
using System.Collections.Generic;

public partial class MushroomAtom : Atom {
	public MushroomAtom() {
		SetTexture((Texture2D)GD.Load("res://Assets/kenney_platformer-art-deluxe/Mushroom expansion/PNG/tallShroom_red.png")); 
	}
	
	public override bool ValidatePlacement(Room room) {
		// Ensure Mushroom is placed on a floor
		return room.HasPrimitiveBelow(GlobalPosition, typeof(Floor));
	}
}

public partial class Mushroom : Primitive
{
	public Mushroom() : base(Vector2.Zero) {	
		Category = PrimitiveCategory.Hazard;
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
