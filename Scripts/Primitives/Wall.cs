using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class WallTile : Atom {
	public WallTile() {
		SetTexture((Texture2D)GD.Load("res://Assets/kenney_platformer-art-deluxe/Base pack/Tiles/stoneCenter.png")); 
		
		// Add a collision shape
		CollisionShape2D collision = new CollisionShape2D();
		RectangleShape2D shape = new RectangleShape2D();
		shape.Size = new Vector2(70, 70); 

		SetCollisionLayerValue(2, true);
		SetCollisionMaskValue(1, true);
		
		collision.Shape = shape;
		AddChild(collision);
	}
	
	public override bool ValidatePlacement(Room room) {
		return true;
	}
}

public partial class Wall : Primitive {

	public Zone zone { get; set; }
	public int width { get; set; }
	public int height { get; set; }

	public Wall() : base(Vector2.Zero) {
		Category = PrimitiveCategory.Environmental;
	}  // Required constructor

	public Wall(Vector2 position) : base(position) { }
	
	public override bool GenerateInRoom(Room room) {
		
		for (int x = 0; x < width; x++) {
			for (int y = 0; y < height; y++){
				WallTile tile = new WallTile();
				tile.GlobalPosition = this.Position + new Vector2(x * 70, y * 70);
				AddAtom(tile);
				//if (!room.AddAtom(tile)) {
					//return false;
				//}
			}
		}
		return room.AddPrimitive(this);
	}
	
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
			Anchor topLeft = new Anchor(pos + offsetUp - offsetSide, orbit, "topLeft");
			Anchor topRight = new Anchor(pos + offsetUp + offsetSide, orbit, "topRight");

			Anchors.Add(topLeft);
			Anchors.Add(topRight);
			InternalPaths.Add(new AnchorConnection(topLeft, topRight));
		}
		
		Anchor left = new Anchor(tiles.First().GlobalPosition - offsetSide + offsetDown, orbit, "left");
		Anchor right = new Anchor(tiles.Last().GlobalPosition + offsetSide + offsetDown, orbit, "right");
		Anchor leftJump = new Anchor(tiles.First().GlobalPosition - offsetSide + (offsetDown * 3), orbit, "leftJump");
		Anchor rightJump = new Anchor(tiles.Last().GlobalPosition + offsetSide + (offsetDown * 3), orbit, "rightJump");
		
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
		Vector2 bottomLeft = topLeft + new Vector2(0, height * 70);
		Vector2 bottomRight = lastTile.GlobalPosition + lastTile.Size / 2;
		Vector2 topRight = bottomRight - new Vector2(0, height * 70);

		ObstructionLines.Add((topLeft, topRight));
		ObstructionLines.Add((topRight, bottomRight));
		ObstructionLines.Add((bottomRight, bottomLeft));
		ObstructionLines.Add((bottomLeft, topLeft));
	}
}
