using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class FloorTile : Atom {
	public FloorTile() {
		SetTexture((Texture2D)GD.Load("res://Assets/kenney_platformer-art-deluxe/Base pack/Tiles/stoneMid.png")); 
		Size = new Vector2(70, 70);

		// Add a collision shape
		CollisionShape2D collision = new CollisionShape2D();
		RectangleShape2D shape = new RectangleShape2D();
		shape.Size = Size; 

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

	public Zone zone { get; set; }
	
	public Floor() : base(Vector2.Zero) {
		Category = PrimitiveCategory.Floor;
	}  // Required constructor

	public Floor(Vector2 position) : base(position) { }
	
	public override void GenerateInRoom(Room room) {
		
		int y = zone.Y + zone.Height - 1;
		for (int x = zone.X; x < zone.X + zone.Width; x++) {
			Vector2 position = new Vector2(x * 70, y * 70); 
			
			FloorTile tile = new FloorTile();
			tile.GlobalPosition = position;
			AddAtom(tile);
			room.AddAtom(tile); // ✅ `AddAtom()` is called here to place each FloorTile atom
		}

		this.Position = new Vector2(zone.X * 70, y * 70); 
		room.AddPrimitive(this);
	}

	//public override void GenerateInRoom(Room room) {
		//
		//for (int x = 0; x < room.Width; x++) {
			//Vector2 position = new Vector2(x * 70, 0); 
			//
			//FloorTile tile = new FloorTile();
			//tile.GlobalPosition = position;
			//AddAtom(tile);
			//room.AddAtom(tile); // ✅ `AddAtom()` is called here to place each FloorTile atom
		//}
//
		////this.GlobalPosition = new Vector2(0, 0); 
		//room.AddPrimitive(this);
	//}
	
	public override void GenerateAnchors()
	{
		Anchors.Clear();

		List<Atom> tiles = GetAtoms(); // This should return the ladder tiles

		if (tiles.Count == 0)
			return;

		// Sort by Y to identify top and bottom
		tiles.Sort((a, b) => a.GlobalPosition.Y.CompareTo(b.GlobalPosition.Y));

		float orbit = 20f; // radius in pixels

		Vector2 offsetUp = new Vector2(0, -tiles.First().Size.Y / 2);
		Vector2 offsetDown = new Vector2(0, tiles.First().Size.Y / 2);
		Vector2 offsetSide = new Vector2(tiles.First().Size.X / 2, 0);
		
		foreach (Atom tile in tiles) {
			Vector2 pos = tile.GlobalPosition;
			Anchors.Add(new Anchor(pos + offsetUp - offsetSide, orbit, "topLeft"));
			Anchors.Add(new Anchor(pos + offsetUp + offsetSide, orbit, "topRight"));
			Anchors.Add(new Anchor(pos - offsetSide, orbit, "left")); 
			Anchors.Add(new Anchor(pos + offsetSide, orbit, "right"));
		}
	}
}
