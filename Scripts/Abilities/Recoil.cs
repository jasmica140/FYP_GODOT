using Godot;
using System;
using ImGuiNET;

public class Recoil : Ability
{
	private float dashDuration = 0.1f;       // Duration of dash
	private float startXPos;       // player starting X position

	// parameters 
	public float distance = 0.05f;
	public float speed = 150.0f; // Velocity during dash
	public float airAcceleration = 400.0f;
	
	public Recoil(PlayerController playerController) : base(playerController){	}

	public override void Activate() {
		// Only activate if not already dashing
		if (!player.isRecoilingRight && !player.isRecoilingLeft) {
			startXPos = player.Position.X; // player x position when starting dash
		}
	}

	public void UpdateRecoil(float delta)
	{
		if (player.isRecoilingRight || player.isRecoilingLeft) {
			
			

			// Check for dash input
			if (player.isRecoilingRight) {
				player.velocity.X = speed; // Move right
			} else if (player.isRecoilingLeft) {
				player.velocity.X = -speed; // Move left
			}
						
			// Check if dash distance has been reached or player hit wall
			if (Math.Abs(startXPos - player.Position.X) >= distance || player.isNearWall()) {
				
				//player.velocity.Y = player.; // start going down

				if (player.isOnFloor() || player.inWater || player.isOnSlope()) { // if player lands on floor
					player.velocity.X = 0; // stop horizontal movement after dash
					
					if (player.isRecoilingRight) { // reset drection
						player.direction = -1; 
					} else if (player.isRecoilingLeft) {
						player.direction = 1; 
					}
					player.setDirection(); // reset direction
					
					player.isRecoilingRight = false; // Reset right recoil flag
					player.isRecoilingLeft = false; // Reset left recoil flag
					player.isHurt = false;
				}
			} else {
				player.velocity.Y = -airAcceleration; // First jump
			}
		}
	}
}
