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
	
	public override bool GenerateInRoom(Room room) {
		
		int y = zone.Y + zone.Height - 1;
		for (int x = zone.X; x < zone.X + zone.Width; x++) {
			Vector2 position = new Vector2(x * 70, y * 70); 
			
			FloorTile tile = new FloorTile();
			tile.GlobalPosition = position;
			AddAtom(tile);
			//room.AddAtom(tile); // ✅ `AddAtom()` is called here to place each FloorTile atom
		}

		this.Position = new Vector2(zone.X * 70, y * 70); 
		return room.AddPrimitive(this);
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
	
	public override void GenerateAnchors(Room room)
	{
		Anchors.Clear();
		InternalPaths.Clear();

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
			Anchor topLeft = new Anchor(pos + offsetUp - offsetSide, orbit, "topLeft", this);
			Anchor topRight = new Anchor(pos + offsetUp + offsetSide, orbit, "topRight", this);

			Anchors.Add(topLeft);
			Anchors.Add(topRight);
			InternalPaths.Add(new AnchorConnection(topLeft, topRight));
		}
		
		Anchor left = new Anchor(tiles.First().GlobalPosition - offsetSide + offsetDown, orbit, "left", this);
		Anchor right = new Anchor(tiles.Last().GlobalPosition + offsetSide + offsetDown, orbit, "right", this);
		Anchor leftJump = new Anchor(tiles.First().GlobalPosition - offsetSide + (offsetDown * 3), orbit, "leftJump", this);
		Anchor rightJump = new Anchor(tiles.Last().GlobalPosition + offsetSide + (offsetDown * 3), orbit, "rightJump", this);
		
		InternalPaths.Add(new AnchorConnection(left, Anchors.First()));
		InternalPaths.Add(new AnchorConnection(left, leftJump));
		InternalPaths.Add(new AnchorConnection(right, Anchors.Last()));
		InternalPaths.Add(new AnchorConnection(right, rightJump));
		
		Anchors.Add(left); 
		Anchors.Add(right);
		Anchors.Add(leftJump); 
		Anchors.Add(rightJump);

		GenerateObstructionLines();
	}
	
	public void GenerateObstructionLines()
	{
		ObstructionLines.Clear();
		List<Atom> tiles = GetAtoms(); // This should return the ladder tiles
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
			
		//foreach (Atom tile in GetAtoms())
		//{
			//Vector2 pos = tile.GlobalPosition;
			//Vector2 size = tile.Size;
//
			//Vector2 topLeft = pos - size / 2;
			//Vector2 topRight = pos + new Vector2(size.X / 2, -size.Y / 2);
			//Vector2 bottomLeft = pos + new Vector2(-size.X / 2, size.Y / 2);
			//Vector2 bottomRight = pos + size / 2;
//
			//ObstructionLines.Add((topLeft, topRight));
			//ObstructionLines.Add((topRight, bottomRight));
			//ObstructionLines.Add((bottomRight, bottomLeft));
			//ObstructionLines.Add((bottomLeft, topLeft));
		//}
	}
}
