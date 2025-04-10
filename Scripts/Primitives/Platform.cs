using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class PlatformTile : Atom {
	public PlatformTile() {
		SetTexture((Texture2D)GD.Load("res://Assets/kenney_platformer-art-deluxe/Base pack/Tiles/stoneHalf.png")); // Replace with actual path
		
		// Add a collision shape
		CollisionShape2D collision = new CollisionShape2D();
		RectangleShape2D shape = new RectangleShape2D();
		shape.Size = new Vector2(70, 40); 
		collision.Shape = shape;
		collision.Position = new Vector2(0, -15); 
		AddChild(collision);
		
		SetCollisionLayerValue(2, true);
		SetCollisionMaskValue(1, true);
	}
	
	public override bool ValidatePlacement(Room room) {
		return true;
	}
}

public partial class Platform : Primitive
{
	public Platform() : base(Vector2.Zero) {
		Category = PrimitiveCategory.Platform;
	}  // Required constructor

	public Platform(Vector2 position) : base(position) {}

	public override void GenerateInRoom(Room room)
	{
		Vector2 position;
		int attempts = 0;
		const int maxAttempts = 10;

		do {
			position = room.GetRandomPosition();
			attempts++;
		} while (room.Primitives.Exists(p => p.GlobalPosition == position) && attempts < maxAttempts);

		if (attempts >= maxAttempts) {
			GD.Print($"⚠️ WARNING: Could not find unique placement for {this.GetType().Name}");
			return;
		}

		PlatformTile atom = new PlatformTile();
		atom.GlobalPosition = position;
		AddAtom(atom);
		room.AddAtom(atom); 
		
		this.Position = position;
		room.AddPrimitive(this);
	}
	
	public override void GenerateAnchors()
	{
		Anchors.Clear();

		List<Atom> tiles = GetAtoms(); // This should return the ladder tiles

		if (tiles.Count == 0)
			return;

		// Sort by Y to identify top and bottom
		tiles.Sort((a, b) => a.GlobalPosition.Y.CompareTo(b.GlobalPosition.Y));

		float orbit = 20f; // radius in pixels

		Vector2 offsetUp = new Vector2(0, -tiles.First().Size.Y / 2);
		Vector2 offsetSide = new Vector2(-tiles.First().Size.X / 2, 0);

		foreach (Atom tile in tiles) {
			Vector2 pos = tile.GlobalPosition;
			Anchors.Add(new Anchor(pos + offsetUp - offsetSide, orbit, "topLeft"));
			Anchors.Add(new Anchor(pos + offsetUp + offsetSide, orbit, "topRight"));
		}
	}
}
