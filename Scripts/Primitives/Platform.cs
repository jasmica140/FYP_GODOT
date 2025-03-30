using Godot;
using System;

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
		
		this.Position = atom.GlobalPosition;
		room.AddPrimitive(this);
	}
}
