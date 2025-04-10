using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class LadderTile : Atom {
	
	public LadderTile() {
		SetTexture((Texture2D)GD.Load("res://Assets/Sprites/Tiles/ladder_mid.png")); // Replace with actual path
		Size = new Vector2(70, 70);
		
		// Add a collision shape
		CollisionShape2D collision = new CollisionShape2D();
		RectangleShape2D shape = new RectangleShape2D();
		shape.Size = new Vector2(Size.X-20, Size.Y); 
		
		SetCollisionLayerValue(3, true);
		SetCollisionMaskValue(1, true);

		collision.Shape = shape;
		AddChild(collision);
		AddToGroup("Ladder");
	}
	
	public override bool ValidatePlacement(Room room) {
		return true;
	}
}

public partial class Ladder : Primitive {

	public Vector2 position { get; set; }
	public int length { get; set; }

	public Ladder() : base(Vector2.Zero) {
		Category = PrimitiveCategory.MovementModifier;
	}  // Required constructor

	public Ladder(Vector2 position) : base(position) {}
	
	public override void GenerateInRoom(Room room) {
		
		for (int y = 0; y < length; y++) {
			LadderTile tile = new LadderTile();
			tile.GlobalPosition = new Vector2(position.X * 70, position.Y * 70) + new Vector2(0, y * 70); 
			AddAtom(tile);
			room.AddAtom(tile); // ✅ `AddAtom()` is called here to place each FloorTile atom
		}
		
		this.Position = new Vector2(position.X * 70, position.Y * 70);
		room.AddPrimitive(this);
	}
	
	//public override void GenerateInRoom(Room room) {
		//List<Vector2> validPositions = room.GetPositionsAboveFloorTiles();
//
		//if (validPositions.Count == 0) {
			//GD.Print($"⚠️ WARNING: No valid floor tile positions found for {this.GetType().Name}");
			//return;
		//}
		//
		//// Pick a random valid position from the list
		//Random random = new Random();
		//Vector2 chosenPosition = validPositions[random.Next(validPositions.Count)];
		//int numOfTiles = random.Next(2, 10);
//
		//for (int y = 0; y < numOfTiles; y++) {
			//Vector2 position =  chosenPosition - new Vector2(0, y * 70); 
			//LadderTile tile = new LadderTile();
			//tile.GlobalPosition = position;
			//AddAtom(tile);
			//room.AddAtom(tile); // ✅ `AddAtom()` is called here to place each FloorTile atom
		//}
		//
		//this.Position = chosenPosition;
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

		Vector2 bottomPos = tiles.First().GlobalPosition;
		Vector2 topPos = tiles.Last().GlobalPosition;

		float orbit = 10f; // radius in pixels

		Vector2 offsetUp = new Vector2(0, -tiles.First().Size.Y / 2);
		Vector2 offsetDown = new Vector2(0, tiles.First().Size.Y / 2);
		Vector2 offsetSide = new Vector2(tiles.First().Size.X / 2, 0);

		// Bottom anchor
		Anchors.Add(new Anchor(bottomPos + offsetDown, orbit, "bottom"));

		// Top anchor
		Anchors.Add(new Anchor(topPos + offsetUp, orbit, "top"));

		// Side anchors
		foreach (Atom tile in tiles)
		{
			Vector2 pos = tile.GlobalPosition;
			Anchors.Add(new Anchor(pos - offsetSide, orbit, "left"));
			Anchors.Add(new Anchor(pos + offsetSide, orbit, "right"));
		}
	}
}
