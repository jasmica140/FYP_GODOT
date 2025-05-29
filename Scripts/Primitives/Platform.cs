using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class PlatformTile : Atom {
	public PlatformTile() {
		// assign texture to tile
		SetTexture((Texture2D)GD.Load("res://Assets/kenney_platformer-art-deluxe/Base pack/Tiles/stoneHalf.png"));
		
		// create collision box
		CollisionShape2D collision = new CollisionShape2D();
		RectangleShape2D shape = new RectangleShape2D();
		shape.Size = new Vector2(70, 40);
		collision.Shape = shape;
		collision.Position = new Vector2(0, -15); 
		AddChild(collision);

		// set physics layers
		SetCollisionLayerValue(2, true);
		SetCollisionMaskValue(1, true);
	}
	
	// no specific placement check required
	public override bool ValidatePlacement(Room room) {
		return true;
	}
}

public partial class Platform : Primitive
{
	public Vector2 position { get; set; }

	public Platform() : base(Vector2.Zero) {
		Category = PrimitiveCategory.Platform;
	}

	public Platform(Vector2 position) : base(position) {}

	// places platform tile and registers atom and primitive to room
	public override bool GenerateInRoom(Room room)
	{
		PlatformTile atom = new PlatformTile();
		atom.GlobalPosition = position;
		AddAtom(atom);
		room.AddAtom(atom);

		this.Position = position;
		return room.AddPrimitive(this);
	}

	// adds anchors above each tile corner for connecting player paths
	public override void GenerateAnchors(Room room)
	{
		Anchors.Clear();

		List<Atom> tiles = GetAtoms();
		if (tiles.Count == 0) return;

		// sort for consistent offsets
		tiles.Sort((a, b) => a.GlobalPosition.Y.CompareTo(b.GlobalPosition.Y));

		float orbit = 20f;

		// offset to top and both sides of platform
		Vector2 offsetUp = new Vector2(0, -tiles.First().Size.Y / 2);
		Vector2 offsetSide = new Vector2(-tiles.First().Size.X / 2, 0);

		foreach (Atom tile in tiles) {
			Vector2 pos = tile.GlobalPosition;
			Anchors.Add(new Anchor(pos + offsetUp - offsetSide, orbit, "topLeft", this));
			Anchors.Add(new Anchor(pos + offsetUp + offsetSide, orbit, "topRight", this));
		}
	}
}
