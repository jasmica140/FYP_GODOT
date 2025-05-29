using Godot;
using System;
using ImGuiNET;

public class WallJump : Ability
{
	public float airAcceleration = 800.0f; 
	public float distance = 150.0f;
	public float gripTime = 1.0f;
	public float gripTimer = 1.0f;
	
	public bool canWallJump = true;
	public bool wallFriction = true;
	
	public WallJump(PlayerController playerController) : base(playerController){	}

	public override void Activate()
	{		
		if (canWallJump) { 
			player.velocity.X = -distance*player.direction; // doesnt work bc its before handle walk in physics process
			player.velocity.Y = -airAcceleration; 
			resetGrip();
			//GD.Print("activating wall jumping");
		} 
	}
	
	public void updateGripTimer (float delta) {
		if (player.isNearWall() && !player.isOnFloor()) {
			// Decrease the grip timer
			gripTimer -= delta;
			
			if (gripTimer < 0) {
				canWallJump = false;
			}			
		} else if (player.isOnFloor()) {
			resetGrip();
		}
	}
	
	private void resetGrip() {
		canWallJump = true;
		gripTimer = gripTime;
	}
	
		
	// Display wall jump paramaters
	public void displayStats () {
		ImGui.DragFloat("jump height", ref airAcceleration);
		ImGui.DragFloat("jump dist", ref distance);
		ImGui.DragFloat("grip time", ref gripTime);

		// Set colors for the toggle switch
		Vector4 onColour = new Vector4(0.2f, 0.7f, 0.2f, 1.0f);   // Green for 'On'
		Vector4 offColour = new Vector4(0.7f, 0.2f, 0.2f, 1.0f);  // Red for 'Off'

		// Begin the toggle button
		ImGui.PushStyleColor(ImGuiCol.Button, ColourToUint(wallFriction ? onColour : offColour));	
		if (ImGui.Button(wallFriction ? "wall friction" : "wall friction", new System.Numerics.Vector2(650, 40)))
		{
			wallFriction = !wallFriction; // Toggle the boolean on button press
		}
		ImGui.PopStyleColor();
	}
	
	private uint ColourToUint(Vector4 colour)
	{
		uint r = (uint)(colour.X * 255.0f);
		uint g = (uint)(colour.Y * 255.0f);
		uint b = (uint)(colour.Z * 255.0f);
		uint a = (uint)(colour.W * 255.0f);

		return (a << 24) | (b << 16) | (g << 8) | r;
	}

}
