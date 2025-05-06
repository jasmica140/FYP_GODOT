using Godot;
using System;
using ImGuiNET;
using System.Linq;

public enum PlayerAbility { Walk, Dash, Jump, DoubleJump, WallJump, Swim }

public partial class PlayerController : CharacterBody2D
{	
	
	public static PlayerController pc { get; private set; }
	public Node CurrentScene { get; set; }
	public Room CurrentRoom { get; set; }
	private Door currentDoor = null;
	private DoorLock currentLock = null;

	// Movement-related variables
	public Vector2 velocity;
	public int direction = 1;
	private float currentHealth = 5.0f; // start with 5 full hearts

	public bool isDashingRight = false;
	public bool isDashingLeft = false;
	public bool isRecoilingRight = false;
	public bool isRecoilingLeft = false;
	public bool touchedHazard = false;
	public bool onLadder = false;
	public bool inWater = false;
	public bool onSlope = false;
	public bool isHurt = false;
	private int laddersTouched = 0;
	private int waterTouched = 0;
	public float moveSpeed = 150.0f;
	public float springJumpSpeed = 1000.0f;
	
	public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

	// containers
	private Node2D _collectiblesContainer;
	private Control _spriteContainer;
	private Node2D _heartContainer;

	// Sprite variables
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
	public RayCast2D _leftSlopeChecker;
	public RayCast2D _rightSlopeChecker;
	public RayCast2D _wallChecker;
	
	// collsion shapes
	public CollisionShape2D _hazardsCollisionShape;
	
	// Ability instances
	public Jump jump;
	public Dash dash;
	public Climb climb;
	public WallJump wallJump;
	public Recoil recoil;
	
	public PlayerController()
	{
		jump = new Jump(this);
		dash = new Dash(this);
		climb = new Climb(this);
		wallJump = new WallJump(this);
		recoil = new Recoil(this);
	}
	
	public override void _Ready()
	{
		pc = this;
		
		Viewport root = GetTree().Root;
		CurrentScene = root.GetChild(root.GetChildCount() - 1);
		
		// get containers
		pc._collectiblesContainer = GetNode<Node2D>("CollectiblesContainer");
		pc._spriteContainer = GetNode<Control>("SpriteContainer");
		pc._heartContainer = GetNode<Node2D>("HeartContainer");

		// get sprite resources
		pc._idleSprite = GetNode<Sprite2D>("SpriteContainer/IdleSprite");
		pc._jumpSprite = GetNode<Sprite2D>("SpriteContainer/JumpSprite");
		pc._duckSprite = GetNode<Sprite2D>("SpriteContainer/DuckSprite");
		pc._rollSprite = GetNode<Sprite2D>("SpriteContainer/RollSprite");
		pc._hurtSprite = GetNode<Sprite2D>("SpriteContainer/HurtSprite");
		pc._swimSprite = GetNode<AnimatedSprite2D>("SpriteContainer/SwimSprite");
		pc._walkSprite = GetNode<AnimatedSprite2D>("SpriteContainer/WalkSprite");
		pc._dashSprite = GetNode<AnimatedSprite2D>("SpriteContainer/DashSprite");
		pc._climbSprite = GetNode<AnimatedSprite2D>("SpriteContainer/ClimbSprite");

		// get collision checkers
		pc._ceilingChecker = GetNode<RayCast2D>("CeilingChecker");
		pc._floorChecker = GetNode<RayCast2D>("FloorChecker");
		pc._leftSlopeChecker = GetNode<RayCast2D>("LeftSlopeChecker");
		pc._rightSlopeChecker = GetNode<RayCast2D>("RightSlopeChecker");
		pc._wallChecker = GetNode<RayCast2D>("WallChecker");
		
		// collision shapes
		pc._hazardsCollisionShape = GetNode<CollisionShape2D>("EnemyChecker/CollisionShape2D");

		
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
			if (Input.IsKeyPressed(Key.Down) && !isOnFloor()) {
				velocity.Y = moveSpeed;
			} else if (Input.IsKeyPressed(Key.Up)) {
				velocity.Y = -moveSpeed;
			}
		}
		
		// handle wall jump
		if (!isOnFloor() && isNearWall() && Input.IsKeyPressed(Key.Space) 
			&& ((direction == -1 && Input.IsKeyPressed(Key.Right)) ||(direction == 1 && Input.IsKeyPressed(Key.Left)))) {
				GD.Print("wall jumping");
			wallJump.Activate();
		} 
		
		// handle walk
		velocity.X = 0;
		if (Input.IsKeyPressed(Key.Left)) {
			direction = -1; // face left
			if (!isNearWall()) { // dont move horizontally if against wall
				velocity.X = -moveSpeed;
			
				if (isOnSlipperyFloor()) { // handle slippery floor
					velocity.X = -moveSpeed * 4;
				} else if (isOnStickyFloor()) { // handle sticky floor
					velocity.X = -moveSpeed / 2;
				} else if (isOnRightSlope()) { // handle slope
					velocity.Y = moveSpeed;
				} else if (isOnLeftSlope()) { // handle slope
					velocity.Y = -moveSpeed;
				}
			}
			
		} else if (Input.IsKeyPressed(Key.Right)) {
			direction = 1; // face right
			if (!isNearWall()) { // dont move horizontally if against wall
				velocity.X = moveSpeed;
				
				if (isOnSlipperyFloor()) { // handle slippery floor
					velocity.X = moveSpeed * 4;
				} else if (isOnStickyFloor()) { // handle sticky floor
					velocity.X = moveSpeed / 2;
				} else if (isOnRightSlope()) { // handle slope
					velocity.Y = -moveSpeed;
				} else if (isOnLeftSlope()) { // handle slope
					velocity.Y = moveSpeed;
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
		if (touchedHazard) {
			recoil.Activate();
			isHurt = true;
			if (direction == 1) {
				isRecoilingLeft = true;
			} else if (direction == -1) {
				isRecoilingRight = true;
			}
		}

		// handle slope
		if (isOnRightSlope()) {
			pc._spriteContainer.RotationDegrees = -45;
		} else if (isOnLeftSlope()) {
			pc._spriteContainer.RotationDegrees = 45;
		} else { // reset when not on slope
			pc._spriteContainer.RotationDegrees = 0;
		}
		
		// handle mushroom jump
		if (isOnMushroom()) {
			velocity.Y = -springJumpSpeed; 
		}
		
		// handle jump
		if (wallJump.gripTimer == wallJump.gripTime){
			if (!jump.variableHeight && Input.IsActionJustPressed("jump")) { // handle jump
				jump.Activate();
				GD.Print("jumping");
			} else if (jump.variableHeight && Input.IsKeyPressed(Key.Space)) {
				jump.Activate();
				GD.Print("jumping");
				if (jump.isJumping) {
					jump.jumpVariableHeight((float)delta);
				}
			} else if (jump.variableHeight && jump.isJumping) { //space was pressed and released
				jump.jumped = true;
				jump.spaceReleased = true;
			}
		}
		
		// handle climb
		if (onLadder && Input.IsKeyPressed(Key.Up)) {
			climb.isClimbingUp = true;
		} else if (onLadder && Input.IsKeyPressed(Key.Down)) {
			climb.isClimbingDown = true;
		} else if (onLadder) {
			climb.Deactivate();
		}

		// handle duck
		if (Input.IsKeyPressed(Key.Down) && isOnFloor() && !inWater) {
			duck = true;
			CapsuleShape2D shape = new CapsuleShape2D();
			shape.Radius = 32;
			shape.Height = 74; 
			pc._hazardsCollisionShape.Shape = shape;
			pc._hazardsCollisionShape.Position = new Vector2(0, 6);
		} else {
			duck = false;
			CapsuleShape2D shape = new CapsuleShape2D();
			shape.Radius = 32;
			shape.Height = 88; 
			pc._hazardsCollisionShape.Shape = shape;
			pc._hazardsCollisionShape.Position = new Vector2(0, -1);
		}
		
		// handle door
		if (currentDoor != null && currentDoor.isOpen && Input.IsActionJustPressed("ui_accept")) {
			GD.Print("opening door");
			//RoomManager.GoToRoomFromDoor(this, currentDoor, this); // assuming 'this' is PlayerController
		} 
		
		if (currentLock != null && Input.IsActionJustPressed("ui_accept")) {
			HandleDoorUnlock();
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
			//pc._slopeChecker.RotationDegrees = 180;
		} else if (velocity.X < 0){
			direction = -1;
			//pc._slopeChecker.RotationDegrees = 270;
		}
		pc._wallChecker.RotationDegrees = 90 * -direction;
	}
	
	public void DecreaseLife(float amount = 0.5f) {
		currentHealth = Mathf.Max(currentHealth - amount, 0); // Clamp at 0

		for (int i = 0; i < 5; i++) {
			AnimatedSprite2D heart = pc._heartContainer.GetChild<AnimatedSprite2D>(i);

			float heartThreshold = currentHealth - i;

			if (heartThreshold >= 1f) {
				heart.Frame = 0; // full
			} else if (heartThreshold >= 0.5f) {
				heart.Frame = 1; // half
			} else {
				heart.Frame = 2; // empty
			}
		}
	}
	

	public bool isNearCeiling() { //check if player is near a ceiling
		return pc._ceilingChecker.IsColliding();
	}
	
	public bool isOnFloor() { //check if player is near floor
		return pc._floorChecker.IsColliding();
	}
	
	
	public bool isOnStickyFloor() { //check if player is on mushroom
		if (!pc._floorChecker.IsColliding())
			return false;
		if (pc._floorChecker.GetCollider() is not PhysicsBody2D body)
			return false;
		
		return body.IsInGroup("StickyFloor");
	}
	
	public bool isOnSlipperyFloor() { //check if player is on mushroom
		if (!pc._floorChecker.IsColliding())
			return false;
		if (pc._floorChecker.GetCollider() is not PhysicsBody2D body)
			return false;
		
		return body.IsInGroup("SlipperyFloor");
	}
	
	
	public bool isNearWall() { //check if player is near a wall
		return pc._wallChecker.IsColliding();
	}
	
	public bool isOnSlope() { //check if player is on slope
		return pc._leftSlopeChecker.IsColliding() || pc._rightSlopeChecker.IsColliding();
	}
	
	public bool isOnRightSlope() { //check if player is on slope
		return pc._rightSlopeChecker.IsColliding();
	}
		
	public bool isOnLeftSlope() { //check if player is on slope
		return pc._leftSlopeChecker.IsColliding();
	}
	
	public bool isOnMushroom() { //check if player is on mushroom
		if (!pc._floorChecker.IsColliding())
			return false;
		if (pc._floorChecker.GetCollider() is not PhysicsBody2D body)
			return false;
		
		return body.IsInGroup("Mushroom");
	}
	
	public bool isNearBlade() { //check if player is near blade
		if (!pc._rightSlopeChecker.IsColliding() && !pc._leftSlopeChecker.IsColliding())
			return false;
		if (pc._rightSlopeChecker.GetCollider() is not PhysicsBody2D body)
			return false;
		//if (pc._leftSlopeChecker.GetCollider() is not PhysicsBody2D body)
			//return false;
			
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
				climb.Deactivate();
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
	
	private bool IsEnemy(Node2D body) {
		return body.IsInGroup("Fish") || body.IsInGroup("FloorBlade") || body.IsInGroup("FullBlade");
	}
	
	public void _on_enemy_checker_body_entered(Node2D body) {
		if (IsEnemy(body)) {
			DecreaseLife();
			if (body.IsInGroup("FloorBlade") || body.IsInGroup("FullBlade")) {
				touchedHazard = true;
			}
		}
	}

	public void _on_enemy_checker_body_exited(Node2D body) {
		touchedHazard = false;
	}
	
	public void _on_door_checker_body_entered(Node2D body)
	{
		if (body.IsInGroup("Door"))
		{
			Primitive doorPrimitive = CurrentRoom.Primitives.FirstOrDefault(p => p.GetAtoms().Contains(body));
			if (doorPrimitive is Door)
			{
				currentDoor = (Door)doorPrimitive;
				GD.Print($"✅ Found door: {currentDoor.Colour}");
			}
			
		} else if (body.IsInGroup("Key")) {
			GD.Print("🔑 Picked up key!");
			Node2D primitivesContainer = CurrentRoom.GetTree().Root.FindChild("PrimitivesContainer", true, false) as Node2D;
			Primitive keyPrimitive = CurrentRoom.Primitives.FirstOrDefault(p => p.GetAtoms().Contains(body));
			primitivesContainer.RemoveChild(keyPrimitive); // Remove key primitive from PrimitivesContainer
			pc._collectiblesContainer.AddChild(keyPrimitive); // Add the key to CollectiblesContainer
			body.Position = new Vector2(0, 30*pc._collectiblesContainer.GetChildCount());
			Atom atom = keyPrimitive.GetAtoms().First(); 
			atom.Scale = new Vector2(0.4f, 0.4f); // Scale down the key
			keyPrimitive.Anchors.Clear(); // remove anchor
			
		} else if (body.IsInGroup("Lock")) {
			Primitive lockPrimitive = CurrentRoom.Primitives.FirstOrDefault(p => p.GetAtoms().Contains(body));
			if (lockPrimitive is DoorLock)
			{
				currentLock = (DoorLock)lockPrimitive;
				GD.Print($"✅ Found lock: {currentLock.Colour}");
			}
		}
	}

	public void _on_door_checker_body_exited(Node2D body)
	{
		if (body.IsInGroup("Door")) {
			currentDoor = null;
		} else if (body.IsInGroup("Lock")) {
			currentLock = null;
		}
	}
	
	private void HandleDoorUnlock() {
		DoorColour lockColour = currentLock.Colour; // Get color of lock

		bool hasKey = pc._collectiblesContainer.GetChildren()
			.OfType<DoorKey>()
			.Any(key => key.Colour == lockColour);

		if (hasKey) {
			GD.Print($"🔓 Unlocked {lockColour} door!");

			// Remove key from container 
			var keyNode = pc._collectiblesContainer.GetChildren()
				.OfType<DoorKey>()
				.First(key => key.Colour == lockColour);
			keyNode.QueueFree();

			// Remove lock from room
			if (currentLock != null) {
				CurrentRoom.Primitives.Remove(currentLock);
				currentLock.QueueFree();
			}

			// Open the corresponding door
			var doorToOpen = CurrentRoom.Primitives
				.OfType<Door>()
				.FirstOrDefault(door => door.Colour == lockColour);

			if (doorToOpen != null) {
				doorToOpen.OpenDoor(CurrentRoom);
			} else {
				GD.PrintErr($"⚠️ No matching door found for color {lockColour}");
			}
		} else {
			GD.Print($"🔒 You don't have the {lockColour} key!");
		}
	}
	
	
	private void _UpdateSpriteRenderer(float velX, float velY, bool ducking) {		
		bool walking = velX != 0;
		bool jumping = velY != 0;
		bool dashing = isDashingLeft != isDashingRight;


		pc._hurtSprite.Visible = isHurt;
		pc._climbSprite.Visible = (climb.isClimbingUp || climb.isClimbingDown || onLadder && !isOnFloor() && !jumping) && !pc._hurtSprite.Visible;
		pc._duckSprite.Visible = ducking && !inWater && !pc._climbSprite.Visible;
		pc._swimSprite.Visible = inWater;
		pc._walkSprite.Visible = (walking && !ducking && !dashing && !pc._climbSprite.Visible) && (isOnFloor() || isOnSlope()) && !pc._hurtSprite.Visible && !inWater;
		pc._idleSprite.Visible = !walking && !jumping && !ducking && !dashing && !pc._climbSprite.Visible && !pc._walkSprite.Visible && !pc._hurtSprite.Visible && !inWater;
		pc._dashSprite.Visible = dashing && !jumping && !ducking && !pc._climbSprite.Visible && !pc._walkSprite.Visible && !pc._hurtSprite.Visible && !inWater;
		pc._jumpSprite.Visible = jumping && !ducking && !pc._climbSprite.Visible && !pc._walkSprite.Visible && !pc._hurtSprite.Visible && !inWater;

		if (pc._hurtSprite.Visible)
		{
			pc._hurtSprite.FlipH = direction == 1;
		} 
		else if (pc._swimSprite.Visible)
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
