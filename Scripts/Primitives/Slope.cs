using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

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
	
	public Slope() : base(Vector2.Zero) {
		Category = PrimitiveCategory.MovementModifier;
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
			AddAtom(slopeTile); // Add the tile to the Floor primitive
			room.AddAtom(slopeTile); // ✅ `AddAtom()` is called here to place each FloorTile atom
			
			Vector2 midSlopePosition =  slopePosition + new Vector2(70, 0); 
			MiddleSlopeTile midSlopeTile = new MiddleSlopeTile();
			midSlopeTile.GlobalPosition = midSlopePosition;
			AddAtom(midSlopeTile);
			room.AddAtom(midSlopeTile); // ✅ `AddAtom()` is called here to place each FloorTile atom
			
			for (int j = 0; j < i; j++) { // maybe i + 1 to add a layer of fillers under slope
				Vector2 fillerTilePosition =  slopePosition + new Vector2(70, (j+1)*70); 
				FillerStoneTile fillerStoneTile = new FillerStoneTile();
				fillerStoneTile.GlobalPosition = fillerTilePosition;
				AddAtom(fillerStoneTile);
				room.AddAtom(fillerStoneTile); // ✅ `AddAtom()` is called here to place each FloorTile atom
			}
		}
		
		this.Position = chosenPosition;
		room.AddPrimitive(this);
	}
	
	public override void GenerateAnchors()
	{
		Anchors.Clear();
		List<Atom> slopeAtoms = GetAtoms();

		if (slopeAtoms.Count == 0)
			return;

		// Get only slope tiles
		var slopeTiles = slopeAtoms.FindAll(a => a is SlopeTile);

		if (slopeTiles.Count == 0)
			return;

		// Sort by X to find the leftmost and rightmost tiles
		slopeTiles.Sort((a, b) => a.GlobalPosition.X.CompareTo(b.GlobalPosition.X));

		Atom startTile = slopeTiles.First();
		Atom endTile = slopeTiles.Last();
		
		Vector2 size = startTile.Size; // assume consistent size
		float orbit = 30f;

		// Anchor positions
		Vector2 bottomLeft = startTile.GlobalPosition + new Vector2(-size.X / 2, size.Y / 2);
		Vector2 topRight = endTile.GlobalPosition + new Vector2((size.X / 2) + size.X, -size.Y / 2);

		// Add anchors
		Anchors.Add(new Anchor(bottomLeft, orbit, "slope_start"));
		Anchors.Add(new Anchor(topRight, orbit, "slope_end"));
	}
}
