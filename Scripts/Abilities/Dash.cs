using Godot;
using System;
using ImGuiNET;

public class Dash : Ability
{
	private float dashDuration = 0.1f;       // Duration of dash
	private float dashTimer = 0;          // Timer for dash
	private float startXPos;       // Duration of dash

	//parameters 
	private float distance = 300.0f;
	private float speed = 1500.0f; // Velocity during dash
	private float cooldown; // time before dash can be used again
	private int direction; // 0 = left; 1= right; 2 = up; 3 = down;
	private bool invincibilityFrame; // is player vulnerable to injury during dash?
	private int energyCost; //cost to use dash?
	
	public Dash(PlayerController playerController) : base(playerController){	}

	public override void Activate()
	{
		// Only activate if not already dashing
		if (!player.isDashingRight && !player.isDashingLeft)
		{
			startXPos = player.Position.X; // player x position when starting dash
			dashTimer = dashDuration; // Start timer
		}
	}

	public void UpdateDash(float delta)
	{
		if (player.isDashingRight || player.isDashingLeft)
		{		
			// Check for dash input
			if (player.isDashingRight) 
			{
				player.velocity.X = speed; // Move right
			} 
			else if (player.isDashingLeft) 
			{
				player.velocity.X = -speed; // Move left
			}
						
			// Check if dash distance has been reached
			if (Math.Abs(startXPos - player.Position.X) >= distance
				|| player.isNearWall()) 
			{
				player.velocity.X = 0; // stop horizontal movement after dash
				
				// Decrease the dash timer
				dashTimer -= delta;
				
				if (dashTimer <= -dashDuration){ // buffer
					player.isDashingRight = false; // Reset right dashing flag
					player.isDashingLeft = false; // Reset left dashing flag
				}
			}
		}
	}
		
	// Display dash paramaters
	public void displayStats() {
		ImGui.DragFloat("dash speed", ref speed);
		ImGui.DragFloat("dash distance", ref distance);
		ImGui.DragFloat("cooloff", ref dashDuration);
	}
}
