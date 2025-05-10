using Godot;
using System;
using ImGuiNET;

public partial class Climb : Ability
{
	public bool isClimbingUp = false;
	public bool isClimbingDown = false;
	public float climbSpeed = 400.0f;
	private bool canJump = true;
	
	public Climb(PlayerController playerController) : base(playerController){	}

	public override void Activate()
	{
		isClimbingUp = true;
	}
	
	public void updateClimbing(float delta) {
		if (isClimbingUp) {
			player.velocity.Y = -climbSpeed;
		} else if (isClimbingDown && !player.isOnFloor()) {
			player.velocity.Y = climbSpeed;
		}
	}
	
	// stop climbing when up is released
	public override void Deactivate() {
		isClimbingUp = false;
		isClimbingDown = false;
		player.velocity.Y = 0;
	}
	
		
	// Display climb paramaters
	public void displayStats() {
		ImGui.DragFloat("climb speed", ref climbSpeed);
	}
}
