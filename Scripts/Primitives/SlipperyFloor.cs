using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class SlipperyFloorTile : Atom {
	public SlipperyFloorTile() {
		SetTexture((Texture2D)GD.Load("res://Assets/kenney_platformer-art-deluxe/Ice expansion/Tiles/tundraMid.png")); 
		Size = new Vector2(70, 70); 
		
		// Add a collision shape
		CollisionShape2D collision = new CollisionShape2D();
		RectangleShape2D shape = new RectangleShape2D();
		shape.Size = Size; 

		SetCollisionLayerValue(2, true);
		SetCollisionMaskValue(1, true);
		
		collision.Shape = shape;
		AddChild(collision);
		AddToGroup("SlipperyFloor");
	}
	
	public override bool ValidatePlacement(Room room) {
		return true;
	}
}

public partial class SlipperyFloor : Primitive {

	public SlipperyFloor() : base(Vector2.Zero) {
		Category = PrimitiveCategory.MovementModifier;
	}  // Required constructor

	public SlipperyFloor(Vector2 position) : base(position) {}

	public override void GenerateInRoom(Room room) {
		// Look for the Floor primitive in the room
		Primitive floorPrimitive = room.Primitives.Find(p => p is Floor);
		List<Atom> tilesToReplace = new List<Atom>();
		List<Atom> floorTiles = floorPrimitive.GetAtoms();
		
		float minX = floorTiles.Min(a => a.GlobalPosition.X);
		float maxX = floorTiles.Max(a => a.GlobalPosition.X);

		Random rng = new Random();
		float x1 = (float)rng.NextDouble() * (maxX - minX) + minX;
		float x2 = (float)rng.NextDouble() * (maxX - minX) + minX;
		
		float lower = Mathf.Min(x1, x2);
		float upper = Mathf.Max(x1, x2);
	
		if (floorPrimitive != null) {
			foreach (Atom floorTile in floorTiles) {
				if (floorTile is FloorTile && floorTile.GlobalPosition.X > lower && floorTile.GlobalPosition.X < upper) {
					
					SlipperyFloorTile tile = new SlipperyFloorTile(); // Create new water tile
					tile.GlobalPosition = floorTile.GlobalPosition;
					AddAtom(tile); 
					room.AddAtom(tile);
					tilesToReplace.Add(floorTile);
				} 
			}
			
			foreach (var floorTile in tilesToReplace) {
				floorPrimitive.RemoveAtom(floorTile);
			}
			
			this.Position = tilesToReplace[0].GlobalPosition;
			room.AddPrimitive(this);
			GD.Print("💧 Floor tiles replaced with water tiles.");
		} else {
			GD.PrintErr("❌ No Floor primitive found in the room!");
		}
	}
	
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
