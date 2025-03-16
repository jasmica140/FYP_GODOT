using Godot;
using System;

public partial class Platform : Primitive
{
	public Platform() : base(Vector2.Zero) {
		Category = PrimitiveCategory.Platform;
		SetTexture((Texture2D)GD.Load("res://Assets/kenney_platformer-art-deluxe/Base pack/Tiles/stoneHalf.png")); // Replace with actual path
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

		this.GlobalPosition = position;
		room.AddPrimitive(this);
	}
	
	public override bool ValidatePlacement(Room room)
	{
		return true;
	}
}
