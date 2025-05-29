using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class RightSlopeTile : Atom {
	public RightSlopeTile() {
		SetTexture((Texture2D)GD.Load("res://Assets/kenney_platformer-art-deluxe/Base pack/Tiles/grassHillLeft.png")); 
		
		// triangle shape simulating right slope
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
		
		// triangle shape simulating left slope
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
		
		// rectangle shape for smoother slope transition
		CollisionShape2D collision = new CollisionShape2D();
		RectangleShape2D shape = new RectangleShape2D();
		shape.Size = new Vector2(70, 70); 

		SetCollisionLayerValue(4, true);
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
		
		// rectangle shape for smoother slope transition
		CollisionShape2D collision = new CollisionShape2D();
		RectangleShape2D shape = new RectangleShape2D();
		shape.Size = new Vector2(70, 70); 

		SetCollisionLayerValue(4, true);
		SetCollisionMaskValue(1, true);
		SetCollisionMaskValue(6, true);
		
		collision.Shape = shape;
		AddChild(collision);
	}
	
	public override bool ValidatePlacement(Room room) {
		return true;
	}
}

public partial class LeftSlope : Primitive {
	public Vector2 position { get; set; } // start position is lower right corner
	public int length { get; set; }
	public Zone zone { get; set; }

	public LeftSlope() : base(Vector2.Zero) {
		Category = PrimitiveCategory.MovementModifier;
	}

	public LeftSlope(Vector2 position) : base(position) {}

	// places left-facing slope tiles, middle tiles, and fillers
	public override bool GenerateInRoom(Room room) {
		for (int i = 0; i < length; i++) {
			Vector2 slopePosition = position + new Vector2(-i * 70, -i * 70); 
			
			LeftSlopeTile slopeTile = new LeftSlopeTile();
			slopeTile.GlobalPosition = slopePosition;
			AddAtom(slopeTile);

			// add middle tile unless last
			if (i != length - 1) {
				Vector2 midSlopePosition = slopePosition - new Vector2(70, 0); 
				MiddleLeftSlopeTile midSlopeTile = new MiddleLeftSlopeTile();
				midSlopeTile.GlobalPosition = midSlopePosition;
				AddAtom(midSlopeTile);
			}

			// add filler tiles under slope tile
			for (int j = 0; j < i; j++) {
				Vector2 fillerTilePosition = slopePosition + new Vector2(-70, (j + 1) * 70); 
				FillerStoneTile fillerStoneTile = new FillerStoneTile();
				fillerStoneTile.GlobalPosition = fillerTilePosition;
				AddAtom(fillerStoneTile);
			}
		}

		this.Position = position;

		if (room.AddPrimitive(this)) {
			attemptPlaceFillersUnderZoneFloor(room);
			return true;
		}
		return false;
	}

	// fills vertical wall under zone floor at end of slope
	private void attemptPlaceFillersUnderZoneFloor(Room room) {
		Wall wall = new Wall();
		wall.Position = new Vector2(zone.X * 70, (zone.Y + zone.Height) * 70);
		wall.width = zone.Width - 1;
		wall.height = length - 1;
		wall.GenerateInRoom(room);
	}

	// adds anchors at bottom and top ends of slope
	public override void GenerateAnchors(Room room) {
		Anchors.Clear();
		InternalPaths.Clear();

		List<Atom> slopeAtoms = GetAtoms();
		var slopeTiles = slopeAtoms.FindAll(a => a is LeftSlopeTile);

		if (slopeTiles.Count == 0)
			return;

		slopeTiles.Sort((a, b) => a.GlobalPosition.X.CompareTo(b.GlobalPosition.X));

		Atom startTile = slopeTiles.Last(); // bottom right
		Atom endTile = slopeTiles.First();  // top left

		Vector2 size = startTile.Size;
		float orbit = 30f;

		Vector2 bottomRight = startTile.GlobalPosition + new Vector2(size.X / 2, size.Y / 2);
		Vector2 topLeft = endTile.GlobalPosition + new Vector2(-(size.X / 2) - size.X + 70, -size.Y / 2);

		Anchor bottomAnchor = new Anchor(bottomRight, orbit, "slope_start", this);
		Anchor topAnchor = new Anchor(topLeft, orbit, "slope_end", this);
		Anchors.Add(bottomAnchor);
		Anchors.Add(topAnchor);
		InternalPaths.Add(new AnchorConnection(bottomAnchor, topAnchor));

		GenerateObstructionLines();
	}

	// adds obstruction lines surrounding slope region
	public void GenerateObstructionLines() {
		ObstructionLines.Clear();
		List<Atom> slopeAtoms = GetAtoms();
		var slopeTiles = slopeAtoms.FindAll(a => a is LeftSlopeTile);

		if (slopeTiles.Count == 0)
			return;

		slopeTiles.Sort((a, b) => a.GlobalPosition.X.CompareTo(b.GlobalPosition.X));

		Atom startTile = slopeTiles.Last();
		Atom endTile = slopeTiles.First();

		Vector2 size = startTile.Size;

		Vector2 slopeTop = endTile.GlobalPosition + new Vector2(-(size.X / 2) - size.X + 70, -size.Y / 2);
		Vector2 slopeBottom = startTile.GlobalPosition + new Vector2(size.X / 2, size.Y / 2);
		Vector2 edgeBottom = slopeBottom - new Vector2((length + 1) * size.X, 0);
		Vector2 edgeTop = edgeBottom - new Vector2(0, (length - 1) * size.X);

		ObstructionLines.Add((slopeBottom, slopeTop));
		ObstructionLines.Add((slopeBottom, edgeBottom));
		ObstructionLines.Add((edgeBottom, edgeTop));
		ObstructionLines.Add((startTile.GlobalPosition, startTile.GlobalPosition + new Vector2(0, 70)));
		ObstructionLines.Add((startTile.GlobalPosition - new Vector2(length * size.X, 0), startTile.GlobalPosition + new Vector2(-length * size.X, 70)));
	}
}

public partial class RightSlope : Primitive {
	public Vector2 position { get; set; } // start position is lower left corner
	public int length { get; set; }
	public Zone zone { get; set; }

	public RightSlope() : base(Vector2.Zero) {
		Category = PrimitiveCategory.MovementModifier;
	}

	public RightSlope(Vector2 position) : base(position) {}

	// places right-facing slope tiles, middle tiles, and fillers
	public override bool GenerateInRoom(Room room) {
		for (int i = 0; i < length; i++) {
			Vector2 slopePosition = position + new Vector2(i * 70, -i * 70); 

			RightSlopeTile slopeTile = new RightSlopeTile();
			slopeTile.GlobalPosition = slopePosition;
			AddAtom(slopeTile);

			// add middle tile unless last
			if (i != length - 1) {
				Vector2 midSlopePosition = slopePosition + new Vector2(70, 0); 
				MiddleRightSlopeTile midSlopeTile = new MiddleRightSlopeTile();
				midSlopeTile.GlobalPosition = midSlopePosition;
				AddAtom(midSlopeTile);
			}

			// add filler tiles under slope tile
			for (int j = 0; j < i; j++) {
				Vector2 fillerTilePosition = slopePosition + new Vector2(70, (j + 1) * 70); 
				FillerStoneTile fillerStoneTile = new FillerStoneTile();
				fillerStoneTile.GlobalPosition = fillerTilePosition;
				AddAtom(fillerStoneTile);
			}
		}

		this.Position = position;

		if (room.AddPrimitive(this)) {
			attemptPlaceFillersUnderZoneFloor(room);
			return true;
		}
		return false;
	}

	// fills vertical wall under zone floor at end of slope
	private void attemptPlaceFillersUnderZoneFloor(Room room) {
		Wall wall = new Wall();
		wall.Position = new Vector2((zone.X + 1) * 70, (zone.Y + zone.Height) * 70);
		wall.width = zone.Width - 1;
		wall.height = length - 1;

		while (wall.GenerateInRoom(room)) {
			// if new floor continues to the right, extend wall again
			Floor floor = room.Primitives.FirstOrDefault(p => p.GetType() == typeof(Floor) && p.Position == wall.Position + new Vector2(wall.width, -70)) as Floor;
			if (floor != null) {
				wall = new Wall();
				wall.Position = new Vector2((floor.zone.X + 1) * 70, (floor.zone.Y + floor.zone.Height) * 70);
				wall.width = floor.zone.Width - 1;
				wall.height = length - 1;
			} else {
				break;
			}
		}
	}

	// adds anchors at bottom and top ends of slope
	public override void GenerateAnchors(Room room) {
		Anchors.Clear();
		InternalPaths.Clear();

		List<Atom> slopeAtoms = GetAtoms();
		var slopeTiles = slopeAtoms.FindAll(a => a is RightSlopeTile);

		if (slopeTiles.Count == 0)
			return;

		slopeTiles.Sort((a, b) => a.GlobalPosition.X.CompareTo(b.GlobalPosition.X));

		Atom startTile = slopeTiles.First(); // bottom left
		Atom endTile = slopeTiles.Last();   // top right

		Vector2 size = startTile.Size;
		float orbit = 30f;

		Vector2 bottomLeft = startTile.GlobalPosition + new Vector2(-size.X / 2, size.Y / 2);
		Vector2 topRight = endTile.GlobalPosition + new Vector2((size.X / 2) + size.X - 70, -size.Y / 2);

		Anchor bottomAnchor = new Anchor(bottomLeft, orbit, "slope_start", this);
		Anchor topAnchor = new Anchor(topRight, orbit, "slope_end", this);
		Anchors.Add(bottomAnchor);
		Anchors.Add(topAnchor);
		InternalPaths.Add(new AnchorConnection(bottomAnchor, topAnchor));

		GenerateObstructionLines();
	}

	// adds obstruction lines surrounding slope region
	public void GenerateObstructionLines() {
		ObstructionLines.Clear();
		List<Atom> slopeAtoms = GetAtoms();
		var slopeTiles = slopeAtoms.FindAll(a => a is RightSlopeTile);

		if (slopeTiles.Count == 0)
			return;

		slopeTiles.Sort((a, b) => a.GlobalPosition.X.CompareTo(b.GlobalPosition.X));

		Atom startTile = slopeTiles.First();
		Atom endTile = slopeTiles.Last();

		Vector2 size = startTile.Size;

		Vector2 slopeTop = endTile.GlobalPosition + new Vector2((size.X / 2) + size.X - 70, -size.Y / 2);
		Vector2 slopeBottom = startTile.GlobalPosition + new Vector2(-size.X / 2, size.Y / 2);
		Vector2 edgeBottom = slopeBottom + new Vector2((length + 1) * size.X, 0);
		Vector2 edgeTop = edgeBottom - new Vector2(0, (length - 1) * size.Y);

		ObstructionLines.Add((slopeBottom, slopeTop));
		ObstructionLines.Add((slopeBottom, edgeBottom));
		ObstructionLines.Add((edgeBottom, edgeTop));
		ObstructionLines.Add((startTile.GlobalPosition, startTile.GlobalPosition + new Vector2(0, 70)));
		ObstructionLines.Add((startTile.GlobalPosition + new Vector2(length * size.X, 0), startTile.GlobalPosition + new Vector2(length * size.X, 70)));
	}
}
