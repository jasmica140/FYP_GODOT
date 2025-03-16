using Godot;
using System;
using System.Collections.Generic;

public partial class FloorTile : Atom {
	public FloorTile() {
		SetTexture((Texture2D)GD.Load("res://Assets/kenney_platformer-art-deluxe/Base pack/Tiles/stoneMid.png")); 
	}
}

public partial class Floor : Primitive {
	public List<FloorTile> floorTiles = new List<FloorTile>();

	public Floor() : base(Vector2.Zero) {
		Category = PrimitiveCategory.Floor;
		SetTexture((Texture2D)GD.Load("res://Assets/kenney_platformer-art-deluxe/Base pack/Tiles/stoneMid.png")); 
	}  // Required constructor

	public Floor(Vector2 position) : base(position) {}
	
	public override void GenerateInRoom(Room room) {
		
		for (int x = 0; x < room.Width; x++) {
			Vector2 position = new Vector2(x * 70, (room.Height - 1) * 70); 
			
			FloorTile tile = new FloorTile();
			tile.GlobalPosition = position;
			floorTiles.Add(tile);
			AddChild(tile); // Add the tile to the Floor primitive
		}

		room.AddPrimitive(this);
	}

	public override bool ValidatePlacement(Room room)
	{
		return true;
	}
}
