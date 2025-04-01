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
	public bool isRecoilingRight = false;
	public bool isRecoilingLeft = false;
	public bool onLadder = false;
	public bool inWater = false;
	public bool onSlope = false;
	public bool isHurt = false;
	private int laddersTouched = 0;
	private int waterTouched = 0;
	public float moveSpeed = 150.0f;

	public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

	// Sprite variables
	private Control _spriteContainer;
	private Sprite2D _idleSprite;
	private Sprite2D _jumpSprite;
	private Sprite2D _duckSprite;
	private Sprite2D _rollSprite;
	private Sprite2D _hurtSprite;
	private AnimatedSprite2D _walkSprite;
	private AnimatedSprite2D _dashSprite;
	private AnimatedSprite2D _climbSprite;
	private AnimatedSprite2D _swimSprite;

	// player child nodes 
	public RayCast2D _ceilingChecker;
	public RayCast2D _floorChecker;
	public RayCast2D _slopeChecker;
	public RayCast2D _wallChecker;
	
	// Ability instances
	public Jump jump;
	public Dash dash;
	public Climb climb;
	public WallJump wallJump;
	public Recoil recoil;
	
	public Node CurrentScene { get; set; }

	public override void _Ready()
	{
		pc = this;
		
		Viewport root = GetTree().Root;
		CurrentScene = root.GetChild(root.GetChildCount() - 1);
		
		// get sprite resources
		pc._spriteContainer = GetNode<Control>("SpriteContainer");
		pc._idleSprite = GetNode<Sprite2D>("SpriteContainer/IdleSprite");
		pc._jumpSprite = GetNode<Sprite2D>("SpriteContainer/JumpSprite");
		pc._duckSprite = GetNode<Sprite2D>("SpriteContainer/DuckSprite");
		pc._rollSprite = GetNode<Sprite2D>("SpriteContainer/RollSprite");
		pc._hurtSprite = GetNode<Sprite2D>("SpriteContainer/HurtSprite");
		pc._swimSprite = GetNode<AnimatedSprite2D>("SpriteContainer/SwimSprite");
		pc._walkSprite = GetNode<AnimatedSprite2D>("SpriteContainer/WalkSprite");
		pc._dashSprite = GetNode<AnimatedSprite2D>("SpriteContainer/DashSprite");
		pc._climbSprite = GetNode<AnimatedSprite2D>("SpriteContainer/ClimbSprite");

		pc._ceilingChecker = GetNode<RayCast2D>("CeilingChecker");
		pc._floorChecker = GetNode<RayCast2D>("FloorChecker");
		pc._slopeChecker = GetNode<RayCast2D>("SlopeChecker");
		pc._wallChecker = GetNode<RayCast2D>("WallChecker");
		
		// initialize abilities
		jump = new Jump(this);
		dash = new Dash(this);
		climb = new Climb(this);
		wallJump = new WallJump(this);
		recoil = new Recoil(this);
	}

	public override void _Process(double delta) {
		Vector2 position = Position;
	}
	
	public override void _PhysicsProcess(double delta) {	
		velocity = Velocity;
		bool duck = false;
		
		// apply gravity if in air
		if (!isOnFloor() && (!isNearWall() || !wallJump.wallFriction || !wallJump.canWallJump)) {
			velocity.Y += gravity * (float)delta;	
		} else if (!isOnFloor() && isNearWall() && wallJump.wallFriction && wallJump.canWallJump) { // slide down wall
			velocity.Y = gravity * (float)delta;
		} else if (isOnFloor() || isOnSlope()) {
			velocity.Y = 0;
		} 
				
		// handle swim 
		if (inWater) {
			velocity.Y = 0;
		}
		
		// handle walk
		velocity.X = 0;
		if (Input.IsKeyPressed(Key.Left)) {
			direction = -1; // face left
			if (!isNearWall()) { // dont move horizontally if against wall
				velocity.X = -moveSpeed;
				if (isOnSlope()) {
					velocity.Y = moveSpeed;
				}
			}
			
		} else if (Input.IsKeyPressed(Key.Right)) {
			direction = 1; // face right
			if (!isNearWall()) { // dont move horizontally if against wall
				velocity.X = moveSpeed;
				if (isOnSlope()) {
					velocity.Y = -moveSpeed;
				}
			}
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
		
		// handle hurt
		if (isNearBlade()) {
			recoil.Activate();
			isHurt = true;
			if (direction == 1) {
				isRecoilingLeft = true;
			} else if (direction == -1) {
				isRecoilingRight = true;
			}
		}

		// handle slope
		if (isOnSlope()) {
			pc._spriteContainer.RotationDegrees = -45;
			pc._spriteContainer.Position = new Vector2(30,-154);
		} else { // reset when not on slope
			pc._spriteContainer.RotationDegrees = 0;
			pc._spriteContainer.Position = Vector2.Zero;
		}
		
		// handle mushroom jump
		if (isOnMushroom()) {
			velocity.Y = -1000; 
		}
		
		// handle wall jump
		if (!isOnFloor() && isNearWall() && Input.IsKeyPressed(Key.Space) 
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
		if (Input.IsKeyPressed(Key.Down)) {
			duck = true;
		}
		
		jump.Deactivate();  // reset jump counter
		wallJump.updateGripTimer((float)delta);
		dash.UpdateDash((float)delta); // Pass delta time to update the dash
		recoil.UpdateRecoil((float)delta); // Pass delta time to update the recoil
		climb.updateClimbing((float)delta);
		 
		setDirection();
		
		_UpdateSpriteRenderer(velocity.X, velocity.Y, duck);
		Velocity = velocity;
		
		MoveAndSlide();
	}
	
	public void moveAndFall() {
		velocity.Y = velocity.Y + gravity;
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
	

	public bool isNearCeiling() { //check if player is near a ceiling
		return pc._ceilingChecker.IsColliding();
	}
	
	public bool isOnFloor() { //check if player is near floor
		return pc._floorChecker.IsColliding();
	}
	
	public bool isNearWall() { //check if player is near a wall
		return pc._wallChecker.IsColliding();
	}
	
	public bool isOnSlope() { //check if player is on slope
		return pc._slopeChecker.IsColliding();
	}
		
	public bool isOnMushroom() { //check if player is on mushroom
		if (!pc._floorChecker.IsColliding())
			return false;
		if (pc._floorChecker.GetCollider() is not PhysicsBody2D body)
			return false;
		
		return body.IsInGroup("Mushroom");
	}
	
	public bool isNearBlade() { //check if player is near blade
		if (!pc._slopeChecker.IsColliding())
			return false;
		if (pc._slopeChecker.GetCollider() is not PhysicsBody2D body)
			return false;
		
		return body.IsInGroup("FloorBlade");
	}

	private bool IsLadder(Node2D body) {
		return body.IsInGroup("Ladder");
	}
	
	public void _on_ladder_checker_body_entered(Node2D body) {
		if (IsLadder(body)) {
			laddersTouched++;
			onLadder = true;
		}
	}

	public void _on_ladder_checker_body_exited(Node2D body) {
		if (IsLadder(body)) {
			laddersTouched = Math.Max(0, laddersTouched - 1);
			
			if (laddersTouched == 0) {
				onLadder = false;
				climb.isClimbing = false;
			}
		}
	}
	
	private bool IsWater(Node2D body) {
		return body.IsInGroup("Water");
	}
	
	public void _on_water_checker_body_entered(Node2D body) {		
		if (IsWater(body)) {
			waterTouched++;
			inWater = true;
		}
	}

	public void _on_water_checker_body_exited(Node2D body) {
		if (IsWater(body)) {
			waterTouched = Math.Max(0, waterTouched - 1);
			
			if (waterTouched == 0) {
				inWater = false;
			}
		}
	}
	
	
	
	private void _UpdateSpriteRenderer(float velX, float velY, bool ducking) {		
		bool walking = velX != 0;
		bool jumping = velY != 0;
		bool dashing = isDashingLeft != isDashingRight;

		pc._duckSprite.Visible = ducking;
		pc._hurtSprite.Visible = isHurt;
		pc._swimSprite.Visible = inWater;
		pc._climbSprite.Visible = (climb.isClimbing || onLadder && !isOnFloor() && !jumping) && !ducking && !pc._hurtSprite.Visible;
		pc._walkSprite.Visible = (walking && !ducking && !dashing && !pc._climbSprite.Visible) && (isOnFloor() || isOnSlope()) && !pc._hurtSprite.Visible && !inWater;
		pc._idleSprite.Visible = !walking && !jumping && !ducking && !dashing && !pc._climbSprite.Visible && !pc._walkSprite.Visible && !pc._hurtSprite.Visible && !inWater;
		pc._dashSprite.Visible = dashing && !jumping && !ducking && !pc._climbSprite.Visible && !pc._walkSprite.Visible && !pc._hurtSprite.Visible && !inWater;
		pc._jumpSprite.Visible = jumping && !ducking && !pc._climbSprite.Visible && !pc._walkSprite.Visible && !pc._hurtSprite.Visible;

		if (pc._swimSprite.Visible)
		{
			pc._swimSprite.Play();
			pc._swimSprite.FlipH = direction == -1;
		} 
		else if (pc._walkSprite.Visible)
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
