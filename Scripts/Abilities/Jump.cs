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

	// params for first jump
	public bool variableHeight = true;
	public float airAcceleration = 400.0f;
	private float airControl;
	private float airBrake;
	public float cutoff = 100.0f;
	private float downGravity;

	// params for double jump
	public bool doubleJump = true;
	public float djAirAcceleration = 400.0f;
	private float doubleJumpDist;
	private float cooldown;
	private float doubleJumpAirControl;

	public Jump(PlayerController playerController) : base(playerController) {}

	public override void Activate()
	{
		// if variable height jump enabled
		if (variableHeight) {

			// first jump
			if (player.isOnFloor() || player.inWater){
				startJumpYPos = player.Position.Y;
				jumpCount++;
				isJumping = true;
			}
			// double jump (airborne + jumpCount == 1 + space was released)
			else if (doubleJump && !player.isOnFloor() && !player.inWater && jumpCount == 1 && spaceReleased)
			{
				startSecondJumpYPos = player.Position.Y;
				jumpCount++;
			} 
		}
		// fixed jump height mode
		else if (!variableHeight) {

			// first jump
			if ((player.isOnFloor() || player.inWater) && jumpCount == 0)
			{
				player.velocity.Y = -airAcceleration;
				jumpCount++;
				jumped = true;
			}
			// double jump
			else if (doubleJump && (!player.isOnFloor() && !player.inWater) && jumpCount == 1)
			{
				player.velocity.Y = -djAirAcceleration;
				jumpCount++;
			} 
		}
	}

	// resets everything after landing
	public override void Deactivate() {
		if (player.velocity.Y == 0 && jumped) {
			jumpCount = 0;
			isJumping = false;
			jumped = false;
			doubleJumped = false;
			spaceReleased = false;
		}
	}
	
	public void jumpVariableHeight(float delta) {
		// holding jump: keep rising until cutoff reached
		if (player.Position.Y - startJumpYPos >= -cutoff && jumpCount == 1 && !jumped) {
			player.velocity.Y = -airAcceleration;
		} 
		else if (player.Position.Y - startJumpYPos < -cutoff) {
			jumped = true;
		} 

		// double jump variable height
		if (player.Position.Y - startSecondJumpYPos >= -cutoff && spaceReleased && jumpCount == 2 && doubleJump && !doubleJumped) {
			player.velocity.Y = -djAirAcceleration;
		}
		else if (player.Position.Y - startSecondJumpYPos < -cutoff) {
			doubleJumped = true;
		} 
	}
	
	// tweak jump values live
	public void displayJumpStats(double delta, Vector2 Position) {
		Vector4 onColour = new Vector4(0.2f, 0.7f, 0.2f, 1.0f);
		Vector4 offColour = new Vector4(0.7f, 0.2f, 0.2f, 1.0f);

		ImGui.PushStyleColor(ImGuiCol.Button, ColourToUint(variableHeight ? onColour : offColour));	
		if (ImGui.Button(variableHeight ? "variable height" : "variable height", new System.Numerics.Vector2(650, 40))) {
			variableHeight = !variableHeight;
		}
		ImGui.PopStyleColor();
		
		ImGui.DragFloat("air acceleration", ref airAcceleration);
		ImGui.DragFloat("cutoff", ref cutoff);
		ImGui.DragFloat("gravity", ref player.gravity);
	}
	
	// tweak double jump values live
	public void displayDoubleJumpStats() {
		Vector4 onColour = new Vector4(0.2f, 0.7f, 0.2f, 1.0f);
		Vector4 offColour = new Vector4(0.7f, 0.2f, 0.2f, 1.0f);

		ImGui.PushStyleColor(ImGuiCol.Button, ColourToUint(doubleJump ? onColour : offColour));	
		if (ImGui.Button(doubleJump ? "double jump" : "double jump", new System.Numerics.Vector2(650, 40))) {
			doubleJump = !doubleJump;
		}
		ImGui.PopStyleColor();

		ImGui.DragFloat("dj air acc", ref djAirAcceleration);
	}

	// helper to convert color to uint
	private uint ColourToUint(Vector4 colour) {
		uint r = (uint)(colour.X * 255.0f);
		uint g = (uint)(colour.Y * 255.0f);
		uint b = (uint)(colour.Z * 255.0f);
		uint a = (uint)(colour.W * 255.0f);

		return (a << 24) | (b << 16) | (g << 8) | r;
	}
}
