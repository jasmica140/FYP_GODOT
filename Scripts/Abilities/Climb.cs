using Godot;
using System;
using ImGuiNET;

public partial class Climb : Ability
{
	public bool isClimbing = false;
	private float climbSpeed = 400.0f;
	private bool canJump = true;
	
	public Climb(PlayerController playerController) : base(playerController){	}

	public override void Activate()
	{
		isClimbing = true;
	}
	
	public void updateClimbing(float delta) {
		if (isClimbing) {
			player.velocity.Y = -climbSpeed;
		}
	}
	
	// stop climbing when up is released
	public override void Deactivate() {
		isClimbing = false;
		player.velocity.Y = 0;
	}
	
		
	// Display climb paramaters
	public void displayStats() {
		ImGui.DragFloat("climb speed", ref climbSpeed);
	}
}
