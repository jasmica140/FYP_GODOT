using Godot;
using System;

public partial class RoomCamera : Camera2D
{
	public float moveSpeed = 500f; // Pixels per second

	public override void _Process(double delta)
	{
		Vector2 inputDirection = Vector2.Zero;

		if (Input.IsActionPressed("ui_right"))
			inputDirection.X += 1;
		if (Input.IsActionPressed("ui_left"))
			inputDirection.X -= 1;
		if (Input.IsActionPressed("ui_down"))
			inputDirection.Y += 1;
		if (Input.IsActionPressed("ui_up"))
			inputDirection.Y -= 1;

		Position += inputDirection.Normalized() * moveSpeed * (float)delta;
	}
}
