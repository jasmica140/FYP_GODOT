using Godot;
using System;
using System.Collections.Generic;

public partial class SlopeTile : Atom {
	public SlopeTile() {
		SetTexture((Texture2D)GD.Load("res://Assets/kenney_platformer-art-deluxe/Base pack/Tiles/rockHillLeft.png")); 
		
		CollisionPolygon2D collision = new CollisionPolygon2D();
		collision.Polygon = new Vector2[] {new Vector2(35, 35), new Vector2(-35, 35), new Vector2(35, -35)};

		SetCollisionLayerValue(4, true);
		SetCollisionLayerValue(2, true);
		SetCollisionMaskValue(1, true);
		
		AddChild(collision);
	}
	
	public override bool ValidatePlacement(Room room) {
		return true;
	}
}

public partial class MiddleSlopeTile : Atom {
	public MiddleSlopeTile() {
		SetTexture((Texture2D)GD.Load("res://Assets/kenney_platformer-art-deluxe/Base pack/Tiles/stoneHillLeft2.png")); 
		
		// Add a collision shape
		CollisionShape2D collision = new CollisionShape2D();
		RectangleShape2D shape = new RectangleShape2D();
		shape.Size = new Vector2(70, 70); 

		SetCollisionLayerValue(4, true);
		//SetCollisionLayerValue(2, true);
		SetCollisionMaskValue(1, true);
		
		collision.Shape = shape;
		AddChild(collision);
	}
	
	public override bool ValidatePlacement(Room room) {
		return true;
	}
}

public partial class Slope : Primitive {
	public List<SlopeTile> slopeTiles = new List<SlopeTile>();
	public List<MiddleSlopeTile> middleSlopeTiles = new List<MiddleSlopeTile>();
	public List<FillerStoneTile> fillerStoneTiles = new List<FillerStoneTile>();

	public Slope() : base(Vector2.Zero) {
		Category = PrimitiveCategory.Platform;
	}  // Required constructor

	public Slope(Vector2 position) : base(position) {}

	public override void GenerateInRoom(Room room) {
		List<Vector2> validPositions = room.GetPositionsAboveFloorTiles();

		if (validPositions.Count == 0) {
			GD.Print($"⚠️ WARNING: No valid floor tile positions found for {this.GetType().Name}");
			return;
		}
		
		// Pick a random valid position from the list
		Random random = new Random();
		Vector2 chosenPosition = validPositions[random.Next(validPositions.Count)];
		int numOfTiles = random.Next(2, 10);

		for (int i = 0; i < numOfTiles; i++) {
			Vector2 slopePosition =  chosenPosition + new Vector2(i * 70, -i * 70); 
			SlopeTile slopeTile = new SlopeTile();
			slopeTile.GlobalPosition = slopePosition;
			slopeTiles.Add(slopeTile);
			AddChild(slopeTile); // Add the tile to the Floor primitive
			room.AddAtom(slopeTile); // ✅ `AddAtom()` is called here to place each FloorTile atom
			
			Vector2 midSlopePosition =  slopePosition + new Vector2(70, 0); 
			MiddleSlopeTile midSlopeTile = new MiddleSlopeTile();
			midSlopeTile.GlobalPosition = midSlopePosition;
			middleSlopeTiles.Add(midSlopeTile);
			AddChild(midSlopeTile); // Add the tile to the Floor primitive
			room.AddAtom(midSlopeTile); // ✅ `AddAtom()` is called here to place each FloorTile atom
			
			for (int j = 0; j < i; j++) {
				Vector2 fillerTilePosition =  slopePosition + new Vector2(70, (j+1)*70); 
				FillerStoneTile fillerStoneTile = new FillerStoneTile();
				fillerStoneTile.GlobalPosition = fillerTilePosition;
				fillerStoneTiles.Add(fillerStoneTile);
				AddChild(fillerStoneTile); // Add the tile to the Floor primitive
				room.AddAtom(fillerStoneTile); // ✅ `AddAtom()` is called here to place each FloorTile atom
			}
		}
		
		room.AddPrimitive(this);
	}
}
