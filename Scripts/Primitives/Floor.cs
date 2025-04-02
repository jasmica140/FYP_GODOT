using Godot;
using System;
using System.Collections.Generic;

public partial class FloorTile : Atom {
	public FloorTile() {
		SetTexture((Texture2D)GD.Load("res://Assets/kenney_platformer-art-deluxe/Base pack/Tiles/stoneMid.png")); 
		
		// Add a collision shape
		CollisionShape2D collision = new CollisionShape2D();
		RectangleShape2D shape = new RectangleShape2D();
		shape.Size = new Vector2(70, 70); 

		SetCollisionLayerValue(2, true);
		SetCollisionMaskValue(1, true);
		
		collision.Shape = shape;
		AddChild(collision);
		AddToGroup("Floor");
	}
	
	public override bool ValidatePlacement(Room room) {
		return true;
	}
}

public partial class Floor : Primitive {

	public Floor() : base(Vector2.Zero) {
		Category = PrimitiveCategory.Floor;
	}  // Required constructor

	public Floor(Vector2 position) : base(position) {}

	public override void GenerateInRoom(Room room) {
		
		for (int x = 0; x < room.Width; x++) {
			Vector2 position = new Vector2(x * 70, 0); 
			
			FloorTile tile = new FloorTile();
			tile.GlobalPosition = position;
			AddAtom(tile);
			room.AddAtom(tile); // ✅ `AddAtom()` is called here to place each FloorTile atom
		}

		//this.GlobalPosition = new Vector2(0, 0); 
		room.AddPrimitive(this);
	}
	
	public override void GenerateAnchors() {}
}
