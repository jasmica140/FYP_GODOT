using Godot;
using System;
using ImGuiNET;

public partial class PlayerController : CharacterBody2D
{
	// states 
	//enum states = {AIR, FLOOR, LADDER, WALL}
	
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
	private Jump jump;
	private Dash dash;
	private Climb climb;
	private WallJump wallJump;

	public override void _Ready()
	{
		// get sprite resources
		_idleSprite = GetNode<Sprite2D>("IdleSprite");
		_jumpSprite = GetNode<Sprite2D>("JumpSprite");
		_duckSprite = GetNode<Sprite2D>("DuckSprite");
		_rollSprite = GetNode<Sprite2D>("RollSprite");
		_walkSprite = GetNode<AnimatedSprite2D>("WalkSprite");
		_dashSprite = GetNode<AnimatedSprite2D>("DashSprite");
		_climbSprite = GetNode<AnimatedSprite2D>("ClimbSprite");

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
		return _wallChecker.IsColliding();
	}
	
	// set direction for wall jumps
	public void setDirection() {
		if (velocity.X > 0) {
			direction = 1;
		} else if (velocity.X < 0){
			direction = -1;
		}
		_wallChecker.RotationDegrees = 90 * -direction;
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

		_duckSprite.Visible = ducking;
		_climbSprite.Visible = (climb.isClimbing || onLadder && !IsOnFloor() && !jumping) && !ducking;
		_idleSprite.Visible = !walking && !jumping && !ducking && !dashing && !_climbSprite.Visible;
		_walkSprite.Visible = walking && !jumping && !ducking && !dashing && !_climbSprite.Visible;
		_dashSprite.Visible = dashing && !jumping && !ducking && !_climbSprite.Visible;
		_jumpSprite.Visible = jumping && !ducking && !_climbSprite.Visible;

		if (_walkSprite.Visible)
		{
			_walkSprite.Play();
			_walkSprite.FlipH = direction == -1;
		} 
		else if (_dashSprite.Visible)
		{
			_dashSprite.Play();
			_dashSprite.FlipH = direction == -1;
		}
		else if (_jumpSprite.Visible)
		{
			_jumpSprite.FlipH = direction == -1;
		}
		else if (_climbSprite.Visible && velY != 0)
		{
			_climbSprite.Play();
		}
		else if (_climbSprite.Visible && velY == 0)
		{
			_climbSprite.Stop();
		} 
		else if (!walking)
		{
			_idleSprite.FlipH = direction == -1;
		} 
	}
}
