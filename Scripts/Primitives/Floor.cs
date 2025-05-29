using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class FloorTile : Atom {
	public FloorTile() {
		// set texture + size
		SetTexture((Texture2D)GD.Load("res://Assets/kenney_platformer-art-deluxe/Base pack/Tiles/grassMid.png")); 
		Size = new Vector2(70, 70);

		// add collision
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
		// always valid
		return true;
	}
}

public partial class Floor : Primitive {

	public Zone zone { get; set; }
	public int Width { get; set; }
	public bool hasEnemy = false;
	
	public Floor() : base(Vector2.Zero) {
		Category = PrimitiveCategory.Floor;
	}

	public Floor(Vector2 position) : base(position) { }
	
	public override bool GenerateInRoom(Room room) {
		// generate floor row across zone width
		Width = zone.Width;
		int y = zone.Y + zone.Height - 1;

		for (int x = zone.X; x < zone.X + zone.Width; x++) {
			Vector2 position = new Vector2(x * 70, y * 70); 
			FloorTile tile = new FloorTile();
			tile.GlobalPosition = position;
			AddAtom(tile);
		}

		this.Position = new Vector2(zone.X * 70, y * 70); 
		return room.AddPrimitive(this);
	}
	
	public override void GenerateAnchors(Room room)
	{
		// top anchors for traversal
		Anchors.Clear();
		InternalPaths.Clear();

		List<Atom> tiles = GetAtoms();
		if (tiles.Count == 0) return;

		tiles.Sort((a, b) => a.GlobalPosition.Y.CompareTo(b.GlobalPosition.Y));

		float orbit = 20f;

		Vector2 offsetUp = new Vector2(0, -tiles.First().Size.Y / 2);
		Vector2 offsetDown = new Vector2(0, tiles.First().Size.Y / 2);
		Vector2 offsetSide = new Vector2(tiles.First().Size.X / 2, 0);
			
		foreach (Atom tile in tiles) {
			Vector2 pos = tile.GlobalPosition;
			Anchor topLeft = new Anchor(pos + offsetUp - offsetSide, orbit, "topLeft", this);
			Anchor topRight = new Anchor(pos + offsetUp + offsetSide, orbit, "topRight", this);

			Anchors.Add(topLeft);
			Anchors.Add(topRight);
			InternalPaths.Add(new AnchorConnection(topLeft, topRight));
		}

		GenerateObstructionLines();
	}
	
	public void GenerateSideAnchors(Room room)
	{
		// side anchors for vertical movement between floors
		List<Atom> tiles = GetAtoms();
		if (tiles.Count == 0) return;

		tiles.Sort((a, b) => a.GlobalPosition.X.CompareTo(b.GlobalPosition.X));
		int tileSize = (int)tiles.First().Size.X;
		float orbit = 20f;

		List<Atom> floorTiles = room.Atoms
			.Where(a => a is FloorTile || a is FillerStoneTile || a is RightSlopeTile || a is LeftSlopeTile || a is MiddleRightSlopeTile || a is MiddleLeftSlopeTile || a is TopWaterTile)
			.OrderBy(a => a.GlobalPosition.Y)
			.ToList();

		void TryGenerateSide(Vector2 offset, Atom tile)
		{
			Vector2 pos = tile.GlobalPosition;

			// skip if something is next to this tile
			if (room.HasAtomAt(pos + offset) || room.HasAtomAt(pos + offset + new Vector2(0, offset.Y != 0 ? 0 : 70)))
				return;

			float xCheck = pos.X + offset.X;
			Atom floorBelow = floorTiles.FirstOrDefault(a =>
				a.GlobalPosition.Y > pos.Y && Mathf.IsEqualApprox(a.GlobalPosition.X, xCheck));

			if (floorBelow == null) return;

			// add anchor path down to floor below
			int steps = (int)((floorBelow.GlobalPosition.Y - pos.Y) / 70);
			for (int i = 0; i < steps; i++)
			{
				Vector2 baseOffset = offset.Normalized() * 35 + new Vector2(0, 70 * i);
				Anchor top = new Anchor(pos + baseOffset - new Vector2(0, 35), orbit, "side", this);
				Anchor bottom = new Anchor(pos + baseOffset + new Vector2(0, 35), orbit, "side", this);
				Anchors.Add(top);
				Anchors.Add(bottom);
				InternalPaths.Add(new AnchorConnection(top, bottom, i < 2));
			}
		}

		TryGenerateSide(new Vector2(-tileSize, 0), tiles.First());
		TryGenerateSide(new Vector2(tileSize, 0), tiles.Last());
	}
	
	public void GenerateObstructionLines()
	{
		// creates collision outline for the whole floor
		ObstructionLines.Clear();
		List<Atom> tiles = GetAtoms();
		Atom firstTile = tiles.First();
		Atom lastTile = tiles.Last();
		
		Vector2 topLeft = firstTile.GlobalPosition - firstTile.Size / 2;
		Vector2 topRight = lastTile.GlobalPosition + new Vector2(lastTile.Size.X / 2, -lastTile.Size.Y / 2);
		Vector2 bottomLeft = firstTile.GlobalPosition + new Vector2(-firstTile.Size.X / 2, firstTile.Size.Y / 2);
		Vector2 bottomRight = lastTile.GlobalPosition + lastTile.Size / 2;

		ObstructionLines.Add((topLeft, topRight));
		ObstructionLines.Add((topRight, bottomRight));
		ObstructionLines.Add((bottomRight, bottomLeft));
		ObstructionLines.Add((bottomLeft, topLeft));
	}
}
