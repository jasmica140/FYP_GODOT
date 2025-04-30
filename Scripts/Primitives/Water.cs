using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class WaterTile : Atom {
	public WaterTile() {
		SetTexture((Texture2D)GD.Load("res://Assets/kenney_platformer-art-deluxe/Base pack/Tiles/liquidWaterTop_mid.png")); 
		
		// Add a collision shape
		CollisionShape2D collision = new CollisionShape2D();
		RectangleShape2D shape = new RectangleShape2D();
		shape.Size = new Vector2(70, 45); 
		
		collision.Shape = shape;
		collision.Position = new Vector2(1, 11);
		
		SetCollisionLayerValue(5, true);
		SetCollisionMaskValue(1, true);
		
		AddChild(collision);
		AddToGroup("Water");
	}
	
	public override bool ValidatePlacement(Room room) {
		return true;
	}
}

public partial class Water : Primitive {

	public Water() : base(Vector2.Zero) {
		Category = PrimitiveCategory.MovementModifier;
	}  // Required constructor

	public Water(Vector2 position) : base(position) {}

	public override bool GenerateInRoom(Room room) {
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
			foreach (Atom atom in floorTiles) {
				if (atom is FloorTile && atom.GlobalPosition.X > lower && atom.GlobalPosition.X < upper) {
					
					WaterTile tile = new WaterTile(); // Create new water tile
					tile.GlobalPosition = atom.GlobalPosition;
					AddAtom(tile); 
					room.AddAtom(tile);
					tilesToReplace.Add(atom);
				} 
			}
			
			foreach (var tile in tilesToReplace) {
				floorPrimitive.RemoveAtom(tile);
			}
			
			this.Position = tilesToReplace[0].GlobalPosition;
			return room.AddPrimitive(this);
			GD.Print("💧 Floor tiles replaced with water tiles.");
		} else {
			GD.PrintErr("❌ No Floor primitive found in the room!");
			return false;
		}
	}
	
	public override void GenerateAnchors(Room room)
	{
		Anchors.Clear();

		List<Atom> tiles = GetAtoms(); // This should return the ladder tiles

		if (tiles.Count == 0)
			return;

		// Sort by Y to identify top and bottom
		tiles.Sort((a, b) => a.GlobalPosition.Y.CompareTo(b.GlobalPosition.Y));

		float orbit = 20f; // radius in pixels

		Vector2 offsetDown = new Vector2(0, tiles.First().Size.Y / 2);
		
		foreach (Atom tile in tiles) {
			Vector2 pos = tile.GlobalPosition;
			Anchors.Add(new Anchor(pos + offsetDown, orbit, "bottom"));
		}
	}
}
