using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class LadderTile : Atom {
	
	public LadderTile() {
		// set texture + size
		SetTexture((Texture2D)GD.Load("res://Assets/Sprites/Tiles/ladder_mid.png"));
		Size = new Vector2(70, 70);
		
		// add collision
		CollisionShape2D collision = new CollisionShape2D();
		RectangleShape2D shape = new RectangleShape2D();
		shape.Size = new Vector2(Size.X - 20, Size.Y); 
		
		SetCollisionLayerValue(3, true);
		SetCollisionMaskValue(1, true);

		collision.Shape = shape;
		AddChild(collision);
		AddToGroup("Ladder");
	}
	
	public override bool ValidatePlacement(Room room) {
		// always valid
		return true;
	}
}

public partial class Ladder : Primitive {

	public Vector2 position { get; set; }
	public int length { get; set; }

	public Ladder() : base(Vector2.Zero) {
		Category = PrimitiveCategory.MovementModifier;
	}

	public Ladder(Vector2 position) : base(position) {}
	
	public override bool GenerateInRoom(Room room) {
		// convert to pixel position
		this.Position = new Vector2(position.X * 70, position.Y * 70);
		
		// check for walls on both sides
		if ((room.HasAtomAt(this.Position + new Vector2(70, 0)) && !room.HasAtomOfTypeAt(this.Position + new Vector2(70, 0), typeof(LadderTile)))
		|| (room.HasAtomAt(this.Position - new Vector2(70, 0)) && !room.HasAtomOfTypeAt(this.Position - new Vector2(70, 0), typeof(LadderTile)))) {
			return false;
		}
		
		// spawn ladder tiles
		for (int y = 0; y < length; y++) {
			LadderTile tile = new LadderTile();
			tile.GlobalPosition = this.Position + new Vector2(0, y * 70);

			// use top sprite
			if (y == 0) {
				tile.SetTexture((Texture2D)GD.Load("res://Assets/Sprites/Tiles/ladder_top.png"));
			}
			// remove collision on bottom
			else if (y == length - 1) {
				foreach (Node child in tile.GetChildren()) {
					if (child is CollisionShape2D) {
						tile.RemoveChild(child);
						child.QueueFree();
						break;
					}
				}
			}
			AddAtom(tile);
		}

		// add floor tile under ladder if missing
		if (!room.HasAtomBelow(new Vector2(position.X * 70, (position.Y + length - 1) * 70), typeof(FloorTile))) {
			FloorTile tile = new FloorTile();
			tile.GlobalPosition = new Vector2(position.X * 70, (position.Y + length) * 70);
			AddAtom(tile);
		}
		
		return room.AddPrimitive(this);
	}
	
	public override void GenerateAnchors(Room room) {
		Anchors.Clear();

		List<Atom> tiles = GetAtoms();
		if (tiles.Count == 0) return;

		// sort by height
		tiles.Sort((a, b) => a.GlobalPosition.Y.CompareTo(b.GlobalPosition.Y));

		Vector2 bottomPos = tiles.First().GlobalPosition;
		Vector2 topPos = tiles.Last().GlobalPosition;

		float orbit = 10f;

		Vector2 offsetUp = new Vector2(0, -tiles.First().Size.Y / 2);
		Vector2 offsetDown = new Vector2(0, tiles.First().Size.Y / 2);
		Vector2 offsetSide = new Vector2(tiles.First().Size.X / 2, 0);

		// add side + center anchors for each tile
		Anchor? prevCenter = null;

		foreach (Atom tile in tiles) {
			Vector2 pos = tile.GlobalPosition;
			Anchor left = new Anchor(pos - offsetSide + offsetDown, orbit, "left", this);
			Anchor right = new Anchor(pos + offsetSide + offsetDown, orbit, "right", this);
			Anchor center = new Anchor(pos + offsetDown, orbit, "center", this);

			Anchors.Add(left);
			Anchors.Add(right);
			Anchors.Add(center);

			InternalPaths.Add(new AnchorConnection(left, center));
			InternalPaths.Add(new AnchorConnection(center, right));

			// connect this tile’s center to previous one
			if (prevCenter != null) {
				InternalPaths.Add(new AnchorConnection(prevCenter, center));
			}

			prevCenter = center;
		}
	}
}
