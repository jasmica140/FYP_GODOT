using Godot;
using ImGuiNET;

public class Jump : Ability
{
	private int jumpCount = 0;
	public bool isJumping = false;
	public bool jumped = false;
	public bool spaceReleased = false;
	private bool doubleJumped = false;
	private float startJumpYPos;
	private float startSecondJumpYPos;

	//parameters (first jump)
	public bool variableHeight = true; // press and hold for longer jumps
	public float airAcceleration = 400.0f;
	private float airControl;
	private float airBrake;
	public float cutoff = 100.0f; //only when variable height is true
	private float downGravity;
	
	//parameters (double jump)
	public bool doubleJump = true; //whether player can perform double jumps
	public float djAirAcceleration = 400.0f;
	private float doubleJumpDist;
	private float cooldown;
	private float doubleJumpAirControl; //adjustability of horizontal movement while in mid-air
	
	public Jump(PlayerController playerController) : base(playerController) { 	}

	public override void Activate()
	{
		if (variableHeight) {
			
			if (player.isOnFloor()){
				startJumpYPos = player.Position.Y;
				jumpCount++;
				isJumping = true;
			}
			else if (doubleJump && !player.isOnFloor() && jumpCount == 1 && spaceReleased) // If we are in the air and jump is pressed again
			{
				GD.Print("started double jump");
				startSecondJumpYPos = player.Position.Y;
				jumpCount++; // Second jump is executed, set count to 2
			} 
		}
		else if (!variableHeight) {
			
			if (player.isOnFloor() && jumpCount == 0)
			{
				player.velocity.Y = -airAcceleration; // First jump
				jumpCount++; // First jump is executed, set count to 1
				jumped = true;
			}
			else if (doubleJump && !player.isOnFloor() && jumpCount == 1) // If we are in the air and jump is pressed again
			{
				player.velocity.Y = -djAirAcceleration; // Double jump 
				jumpCount++; // Second jump is executed, set count to 2
			} 
		}
	}

	// Reset jump count when landing on the floor
	public override void Deactivate() {
		if (player.velocity.Y == 0 && jumped) {
			jumpCount = 0; // Reset when player touches floor
			isJumping = false;
			jumped = false;
			doubleJumped = false;
			spaceReleased = false;
		}
	}
	
	public void jumpVariableHeight (float delta) {
		if (player.Position.Y - startJumpYPos >= -cutoff && jumpCount == 1 && !jumped) {
			player.velocity.Y = -airAcceleration; // first jump

		} else if (player.Position.Y - startJumpYPos < -cutoff) { // cutoff position was reached
			jumped = true;
		} 

		if (player.Position.Y - startSecondJumpYPos >= -cutoff && spaceReleased && jumpCount == 2 && doubleJump && !doubleJumped) {
			player.velocity.Y = -djAirAcceleration; // second jump
			
		}  else if (player.Position.Y - startSecondJumpYPos < -cutoff) { // cutoff position was reached
			doubleJumped = true;
		} 
	}
	
	// Display jump paramaters
	public void displayJumpStats (double delta, Vector2 Position) {
		// Set colors for the toggle switch
		Vector4 onColour = new Vector4(0.2f, 0.7f, 0.2f, 1.0f);   // Green for 'On'
		Vector4 offColour = new Vector4(0.7f, 0.2f, 0.2f, 1.0f);  // Red for 'Off'

		// Begin the toggle button
		ImGui.PushStyleColor(ImGuiCol.Button, ColourToUint(variableHeight ? onColour : offColour));	
		if (ImGui.Button(variableHeight ? "variable height" : "variable height", new System.Numerics.Vector2(650, 40)))
		{
			variableHeight = !variableHeight; // Toggle the boolean on button press
		}
		ImGui.PopStyleColor();
		
		ImGui.DragFloat("air acceleration", ref airAcceleration);
		ImGui.DragFloat("cutoff", ref cutoff);
		ImGui.DragFloat("gravity", ref player.gravity);

	}
	
		// Display double jump paramaters
	public void displayDoubleJumpStats () {
		// Set colors for the toggle switch
		Vector4 onColour = new Vector4(0.2f, 0.7f, 0.2f, 1.0f);   // Green for 'On'
		Vector4 offColour = new Vector4(0.7f, 0.2f, 0.2f, 1.0f);  // Red for 'Off'
		
		// Begin the toggle button
		ImGui.PushStyleColor(ImGuiCol.Button, ColourToUint(doubleJump ? onColour : offColour));	
		if (ImGui.Button(doubleJump ? "double jump" : "double jump", new System.Numerics.Vector2(650, 40)))
		{
			doubleJump = !doubleJump; // Toggle the boolean on button press
		}
		ImGui.PopStyleColor();
		
		ImGui.DragFloat("dj air acc", ref djAirAcceleration);

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
