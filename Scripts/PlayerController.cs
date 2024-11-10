using Godot;
using System;
using ImGuiNET;

public partial class PlayerController : CharacterBody2D
{	
	
	public static PlayerController pc { get; private set; }

	// Movement-related variables
	public Vector2 velocity;
	public int direction = 1;
	
	public bool isDashingRight = false;
	public bool isDashingLeft = false;
	public bool onLadder = false;
	public float moveSpeed = 150.0f;

	public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

	// Sprite variables
	private Sprite2D _idleSprite;
	private Sprite2D _jumpSprite;
	private Sprite2D _duckSprite;
	private Sprite2D _rollSprite;
	private AnimatedSprite2D _walkSprite;
	private AnimatedSprite2D _dashSprite;
	private AnimatedSprite2D _climbSprite;

	// player child nodes 
	public RayCast2D _wallChecker;
	
	// Ability instances
	public Jump jump;
	public Dash dash;
	public Climb climb;
	public WallJump wallJump;
	
	public Node CurrentScene { get; set; }

	public override void _Ready()
	{
		pc = this;
		
		Viewport root = GetTree().Root;
		CurrentScene = root.GetChild(root.GetChildCount() - 1);
		
		// get sprite resources
		pc._idleSprite = GetNode<Sprite2D>("IdleSprite");
		pc._jumpSprite = GetNode<Sprite2D>("JumpSprite");
		pc._duckSprite = GetNode<Sprite2D>("DuckSprite");
		pc._rollSprite = GetNode<Sprite2D>("RollSprite");
		pc._walkSprite = GetNode<AnimatedSprite2D>("WalkSprite");
		pc._dashSprite = GetNode<AnimatedSprite2D>("DashSprite");
		pc._climbSprite = GetNode<AnimatedSprite2D>("ClimbSprite");

		_wallChecker = GetNode<RayCast2D>("WallChecker");

		// initialize abilities
		jump = new Jump(this);
		dash = new Dash(this);
		climb = new Climb(this);
		wallJump = new WallJump(this);
		
		 if (DisplayServer.WindowGetVsyncMode() == DisplayServer.VSyncMode.Disabled)
		{
			int refreshRate = (int)DisplayServer.ScreenGetRefreshRate();
			Engine.MaxFps = refreshRate > 0 ? refreshRate : 60;
		}

#if IMGUI
		var io = ImGui.GetIO();
		io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;
#endif
	}

	public override void _Process(double delta)
	{
		Vector2 position = Position;
#if IMGUI
	ImGui.Begin("Abilities", ImGuiWindowFlags.AlwaysAutoResize); // Start a main container window

	// Call each ability’s display function in the desired order
	if (ImGui.CollapsingHeader("Jump"))
	{
		jump.displayJumpStats(delta, Position);
	}
	
	if (ImGui.CollapsingHeader("Double Jump"))
	{
		jump.displayDoubleJumpStats();
	}

	if (ImGui.CollapsingHeader("Wall Jump"))
	{
		wallJump.displayStats();
	}

	if (ImGui.CollapsingHeader("Dash"))
	{
		dash.displayStats();
	}
	
	if (ImGui.CollapsingHeader("Climb"))
	{
		climb.displayStats();
	}

	ImGui.End(); // End the main container window
#endif
	}
	
	public override void _PhysicsProcess(double delta)
	{	
		velocity = Velocity;
		bool duck = false;
		
		// apply gravity if in air
		if (!IsOnFloor() && (!isNearWall() || !wallJump.wallFriction || !wallJump.canWallJump)) {
			velocity.Y += gravity * (float)delta;	
		} else if (!IsOnFloor() && isNearWall() && wallJump.wallFriction && wallJump.canWallJump) { // slide down wall
			velocity.Y = gravity * (float)delta;
		}
		
		// handle walk
		velocity.X = 0;
		if (Input.IsKeyPressed(Key.Left))
		{
			velocity.X = -moveSpeed;
		}
		else if (Input.IsKeyPressed(Key.Right))
		{
			velocity.X = moveSpeed;
		}

		// handle right dash
		if (Input.IsActionJustPressed("dashRight")) {
			dash.Activate();
			isDashingRight = true;
		}
		
		// handle left dash
		if (Input.IsActionJustPressed("dashLeft")) {
			dash.Activate();
			isDashingLeft = true;
		}

		// handle wall jump
		if (!IsOnFloor() && isNearWall() && Input.IsKeyPressed(Key.Space) 
			&& (Input.IsKeyPressed(Key.Right) || Input.IsKeyPressed(Key.Left))) {
			wallJump.Activate();
		}
		
		// handle jump
		if (!jump.variableHeight && Input.IsActionJustPressed("jump")) {
			jump.Activate();
		} else if (jump.variableHeight && Input.IsKeyPressed(Key.Space)) {
			jump.Activate();
			if (jump.isJumping) {
				jump.jumpVariableHeight((float)delta);
			}
		} else if (jump.variableHeight && jump.isJumping) { //space was pressed and released
			jump.jumped = true;
			jump.spaceReleased = true;
		}
		
		// handle climb
		if (onLadder && Input.IsKeyPressed(Key.Up)) {
			climb.Activate();
		} else if (onLadder) {
			climb.Deactivate();
		}

		// handle duck
		if (Input.IsKeyPressed(Key.Down))
		{
			duck = true;
		}
		
		
		jump.Deactivate();  // reset jump counter
		wallJump.updateGripTimer((float)delta);
		dash.UpdateDash((float)delta); // Pass delta time to update the dash
		climb.updateClimbing((float)delta);
		
		setDirection();
		
		_UpdateSpriteRenderer(velocity.X, velocity.Y, duck);
		Velocity = velocity;
		
		MoveAndSlide();
	}
	
	// 
	public void moveAndFall() {
		velocity.Y = velocity.Y + gravity;
	}
	
	//check if player is near a wall
	public bool isNearWall() {
		return pc._wallChecker.IsColliding();
	}
	
	// set direction for wall jumps
	public void setDirection() {
		if (velocity.X > 0) {
			direction = 1;
		} else if (velocity.X < 0){
			direction = -1;
		}
		pc._wallChecker.RotationDegrees = 90 * -direction;
	}

	public void _on_ladder_checker_body_entered (Node2D body) {
		onLadder = true;
	}

	public void _on_ladder_checker_body_exited (Node2D body) {
		onLadder = false;
		climb.isClimbing = false;
	}
	
	private void _UpdateSpriteRenderer(float velX, float velY, bool ducking)
	{		
		bool walking = velX != 0;
		bool jumping = velY != 0;
		bool dashing = isDashingLeft != isDashingRight;

		pc._duckSprite.Visible = ducking;
		pc._climbSprite.Visible = (climb.isClimbing || onLadder && !IsOnFloor() && !jumping) && !ducking;
		pc._idleSprite.Visible = !walking && !jumping && !ducking && !dashing && !pc._climbSprite.Visible;
		pc._walkSprite.Visible = walking && !jumping && !ducking && !dashing && !pc._climbSprite.Visible;
		pc._dashSprite.Visible = dashing && !jumping && !ducking && !pc._climbSprite.Visible;
		pc._jumpSprite.Visible = jumping && !ducking && !pc._climbSprite.Visible;

		if (pc._walkSprite.Visible)
		{
			pc._walkSprite.Play();
			pc._walkSprite.FlipH = direction == -1;
		} 
		else if (pc._dashSprite.Visible)
		{
			pc._dashSprite.Play();
			pc._dashSprite.FlipH = direction == -1;
		}
		else if (pc._jumpSprite.Visible)
		{
			pc._jumpSprite.FlipH = direction == -1;
		}
		else if (pc._climbSprite.Visible && velY != 0)
		{
			pc._climbSprite.Play();
		}
		else if (pc._climbSprite.Visible && velY == 0)
		{
			pc._climbSprite.Stop();
		} 
		else if (!walking)
		{
			pc._idleSprite.FlipH = direction == -1;
		} 
	}
	
	public void GotoScene(string path) {
		CallDeferred(MethodName.DeferredGotoScene, path);
	}
	
	public void DeferredGotoScene(string path) {
		// It is now safe to remove the current scene.
		CurrentScene.Free();

		// Load a new scene.
		var nextScene = GD.Load<PackedScene>(path);

		// Instance the new scene.
		CurrentScene = nextScene.Instantiate();

		// Add it to the active scene, as child of root.
		GetTree().Root.AddChild(CurrentScene);

		// Optionally, to make it compatible with the SceneTree.change_scene_to_file() API.
		GetTree().CurrentScene = CurrentScene;
	}
}
