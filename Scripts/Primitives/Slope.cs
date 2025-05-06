using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class RightSlopeTile : Atom {
	public RightSlopeTile() {
		SetTexture((Texture2D)GD.Load("res://Assets/kenney_platformer-art-deluxe/Base pack/Tiles/grassHillLeft.png")); 
		
		CollisionPolygon2D collision = new CollisionPolygon2D();
		collision.Polygon = new Vector2[] {new Vector2(35, 35), new Vector2(-35, 35), new Vector2(35, -35)};

		SetCollisionLayerValue(4, true);
		SetCollisionLayerValue(2, true);
		SetCollisionMaskValue(1, true);
		SetCollisionMaskValue(6, true);
		
		AddChild(collision);
	}
	
	public override bool ValidatePlacement(Room room) {
		return true;
	}
}

public partial class LeftSlopeTile : Atom {
	public LeftSlopeTile() {
		SetTexture((Texture2D)GD.Load("res://Assets/kenney_platformer-art-deluxe/Base pack/Tiles/grassHillRight.png")); 
		
		CollisionPolygon2D collision = new CollisionPolygon2D();
		collision.Polygon = new Vector2[] {new Vector2(35, 35), new Vector2(-35, 35), new Vector2(-35, -35)};

		SetCollisionLayerValue(4, true);
		SetCollisionLayerValue(2, true);
		SetCollisionMaskValue(1, true);
		SetCollisionMaskValue(6, true);
		
		AddChild(collision);
	}
	
	public override bool ValidatePlacement(Room room) {
		return true;
	}
}

public partial class MiddleRightSlopeTile : Atom {
	public MiddleRightSlopeTile() {
		SetTexture((Texture2D)GD.Load("res://Assets/kenney_platformer-art-deluxe/Base pack/Tiles/grassHillLeft2.png")); 
		
		// Add a collision shape
		CollisionShape2D collision = new CollisionShape2D();
		RectangleShape2D shape = new RectangleShape2D();
		shape.Size = new Vector2(70, 70); 

		SetCollisionLayerValue(4, true);
		//SetCollisionLayerValue(2, true);
		SetCollisionMaskValue(1, true);
		SetCollisionMaskValue(6, true);
		
		collision.Shape = shape;
		AddChild(collision);
	}
	
	public override bool ValidatePlacement(Room room) {
		return true;
	}
}

public partial class MiddleLeftSlopeTile : Atom {
	public MiddleLeftSlopeTile() {
		SetTexture((Texture2D)GD.Load("res://Assets/kenney_platformer-art-deluxe/Base pack/Tiles/grassHillRight2.png")); 
		
		// Add a collision shape
		CollisionShape2D collision = new CollisionShape2D();
		RectangleShape2D shape = new RectangleShape2D();
		shape.Size = new Vector2(70, 70); 

		SetCollisionLayerValue(4, true);
		//SetCollisionLayerValue(2, true);
		SetCollisionMaskValue(1, true);
		SetCollisionMaskValue(6, true);
		
		collision.Shape = shape;
		AddChild(collision);
	}
	
	public override bool ValidatePlacement(Room room) {
		return true;
	}
}

public partial class RightSlope : Primitive {
	
	public Vector2 position { get; set; } // start position is lower left corner
	public int length { get; set; }
	public Zone zone { get; set; }

	public RightSlope() : base(Vector2.Zero) {
		Category = PrimitiveCategory.MovementModifier;
	}  // Required constructor

	public RightSlope(Vector2 position) : base(position) {}

	public override bool GenerateInRoom(Room room) {
		
		for (int i = 0; i < length; i++) {
			Vector2 slopePosition =  position + new Vector2(i * 70, -i * 70); 
			RightSlopeTile slopeTile = new RightSlopeTile();
			slopeTile.GlobalPosition = slopePosition;
			AddAtom(slopeTile); // Add the tile to the primitive
			//room.AddAtom(slopeTile); 
			
			if (i != length - 1)  {
				Vector2 midSlopePosition =  slopePosition + new Vector2(70, 0); 
				MiddleRightSlopeTile midSlopeTile = new MiddleRightSlopeTile();
				midSlopeTile.GlobalPosition = midSlopePosition;
				AddAtom(midSlopeTile);
				//room.AddAtom(midSlopeTile); 
			}
			
			for (int j = 0; j < i; j++) { // maybe i + 1 to add a layer of fillers under slope
				Vector2 fillerTilePosition =  slopePosition + new Vector2(70, (j+1)*70); 
				FillerStoneTile fillerStoneTile = new FillerStoneTile();
				fillerStoneTile.GlobalPosition = fillerTilePosition;
				AddAtom(fillerStoneTile);
				//room.AddAtom(fillerStoneTile); // ✅ `AddAtom()` is called here to place each FloorTile atom
			}
				
		}
		
		this.Position = position;

		if (room.AddPrimitive(this)) {
			attemptPlaceFillersUnderZoneFloor(room);
			return true;
		}

		return false;
	}
	
	private void attemptPlaceFillersUnderZoneFloor(Room room) {
		Wall wall = new Wall();
		wall.Position = new Vector2((zone.X + 1)* 70, (zone.Y + zone.Height) * 70);
		wall.width = zone.Width - 1;
		wall.height = length - 1;
		wall.GenerateInRoom(room);
	}
	
	public override void GenerateAnchors(Room room)
	{
		Anchors.Clear();
		InternalPaths.Clear();

		List<Atom> slopeAtoms = GetAtoms();

		if (slopeAtoms.Count == 0)
			return;

		// Get only slope tiles
		var slopeTiles = slopeAtoms.FindAll(a => a is RightSlopeTile);

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
		Vector2 topRight = endTile.GlobalPosition + new Vector2((size.X / 2) + size.X - 70, -size.Y / 2);

		// Add anchors
		Anchor bottomAnchor = new Anchor(bottomLeft, orbit, "slope_start", this);
		Anchor topAnchor = new Anchor(topRight, orbit, "slope_end", this);
		Anchors.Add(bottomAnchor);
		Anchors.Add(topAnchor);
		InternalPaths.Add(new AnchorConnection(bottomAnchor, topAnchor));
		
		GenerateObstructionLines();
	}
	
	public void GenerateObstructionLines()
	{
		ObstructionLines.Clear();
		List<Atom> slopeAtoms = GetAtoms();

		if (slopeAtoms.Count == 0)
			return;

		// Get only slope tiles
		var slopeTiles = slopeAtoms.FindAll(a => a is RightSlopeTile);

		if (slopeTiles.Count == 0)
			return;

		// Sort by X to find the leftmost and rightmost tiles
		slopeTiles.Sort((a, b) => a.GlobalPosition.X.CompareTo(b.GlobalPosition.X));

		Atom startTile = slopeTiles.First();
		Atom endTile = slopeTiles.Last();
		
		Vector2 size = startTile.Size; // assume consistent size
		
		// Anchor positions
		Vector2 slopeTop = endTile.GlobalPosition + new Vector2((size.X / 2) + size.X - 70, -size.Y / 2);
		Vector2 slopeBottom = startTile.GlobalPosition + new Vector2(-size.X / 2, size.Y / 2);
		Vector2 edgeBottom = slopeBottom + new Vector2((length + 1)* size.X, 0);
		Vector2 edgeTop = edgeBottom - new Vector2(0, (length - 1) * size.Y );
		
		ObstructionLines.Add((slopeBottom, slopeTop));
		ObstructionLines.Add((slopeBottom, edgeBottom));
		ObstructionLines.Add((edgeBottom, edgeTop));
	}
}

public partial class LeftSlope : Primitive {
	
	public Vector2 position { get; set; } // start position is lower right corner
	public int length { get; set; }
	public Zone zone { get; set; }

	public LeftSlope() : base(Vector2.Zero) {
		Category = PrimitiveCategory.MovementModifier;
	}  // Required constructor

	public LeftSlope(Vector2 position) : base(position) {}

	public override bool GenerateInRoom(Room room) {
		
		for (int i = 0; i < length; i++) {
			Vector2 slopePosition =  position + new Vector2(-i * 70, -i * 70); 
			LeftSlopeTile slopeTile = new LeftSlopeTile();
			slopeTile.GlobalPosition = slopePosition;
			AddAtom(slopeTile); // Add the tile to the primitive
			//room.AddAtom(slopeTile); 
			
			if (i != length - 1)  {
				Vector2 midSlopePosition =  slopePosition - new Vector2(70, 0); 
				MiddleLeftSlopeTile midSlopeTile = new MiddleLeftSlopeTile();
				midSlopeTile.GlobalPosition = midSlopePosition;
				AddAtom(midSlopeTile);
				//room.AddAtom(midSlopeTile); 
			}

			for (int j = 0; j < i; j++) { // maybe i + 1 to add a layer of fillers under slope
				Vector2 fillerTilePosition =  slopePosition + new Vector2(-70, (j+1)*70); 
				FillerStoneTile fillerStoneTile = new FillerStoneTile();
				fillerStoneTile.GlobalPosition = fillerTilePosition;
				AddAtom(fillerStoneTile);
				//room.AddAtom(fillerStoneTile); // ✅ `AddAtom()` is called here to place each FloorTile atom
			}
			
		}
		
		this.Position = position;

		if (room.AddPrimitive(this)) {
			attemptPlaceFillersUnderZoneFloor(room);
			return true;
		}

		return false;
	}
	
	private void attemptPlaceFillersUnderZoneFloor(Room room) {
		Wall wall = new Wall();
		wall.Position = new Vector2(zone.X * 70, (zone.Y + zone.Height) * 70);
		wall.width = zone.Width - 1;
		wall.height = length - 1;
		wall.GenerateInRoom(room);
	}
	
	public override void GenerateAnchors(Room room)
	{
		Anchors.Clear();
		InternalPaths.Clear();
		List<Atom> slopeAtoms = GetAtoms();

		if (slopeAtoms.Count == 0)
			return;

		// Get only slope tiles
		var slopeTiles = slopeAtoms.FindAll(a => a is LeftSlopeTile);

		if (slopeTiles.Count == 0)
			return;

		// Sort by X to find the leftmost and rightmost tiles
		slopeTiles.Sort((a, b) => a.GlobalPosition.X.CompareTo(b.GlobalPosition.X));

		Atom startTile = slopeTiles.Last();
		Atom endTile = slopeTiles.First();
		
		Vector2 size = startTile.Size; // assume consistent size
		float orbit = 30f;

		// Anchor positions
		Vector2 bottomRight = startTile.GlobalPosition + new Vector2(size.X / 2, size.Y / 2);
		Vector2 topLeft = endTile.GlobalPosition + new Vector2(-(size.X / 2) - size.X + 70, -size.Y / 2);

		Vector2 slopeTop = endTile.GlobalPosition + new Vector2(-(size.X / 2) - size.X + 70, -size.Y / 2);
		Vector2 slopeBottom = startTile.GlobalPosition + new Vector2(size.X / 2, (size.Y / 2));
		
		// Add anchors
		Anchor bottomAnchor = new Anchor(bottomRight, orbit, "slope_start", this);
		Anchor topAnchor = new Anchor(topLeft, orbit, "slope_end", this);
		Anchors.Add(bottomAnchor);
		Anchors.Add(topAnchor);
		InternalPaths.Add(new AnchorConnection(bottomAnchor, topAnchor));

		GenerateObstructionLines();
	}
	
	public void GenerateObstructionLines()
	{
		ObstructionLines.Clear();
		List<Atom> slopeAtoms = GetAtoms();

		if (slopeAtoms.Count == 0)
			return;

		// Get only slope tiles
		var slopeTiles = slopeAtoms.FindAll(a => a is LeftSlopeTile);

		if (slopeTiles.Count == 0)
			return;

		// Sort by X to find the leftmost and rightmost tiles
		slopeTiles.Sort((a, b) => a.GlobalPosition.X.CompareTo(b.GlobalPosition.X));

		Atom startTile = slopeTiles.Last();
		Atom endTile = slopeTiles.First();
		
		Vector2 size = startTile.Size; // assume consistent size

		// Anchor positions
		Vector2 slopeTop = endTile.GlobalPosition + new Vector2(-(size.X / 2) - size.X + 70, -size.Y / 2);
		Vector2 slopeBottom = startTile.GlobalPosition + new Vector2(size.X / 2, (size.Y / 2));
		Vector2 edgeBottom = slopeBottom - new Vector2((length + 1) * size.X, 0);
		Vector2 edgeTop = edgeBottom - new Vector2(0, (length - 1) * size.X );

		ObstructionLines.Add((slopeBottom, slopeTop));
		ObstructionLines.Add((slopeBottom, edgeBottom));
		ObstructionLines.Add((edgeBottom, edgeTop));
	}
}
