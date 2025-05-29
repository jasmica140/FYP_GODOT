using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
public partial class SlugAtom : Atom {
	
	public float speed = 100f;
	public int direction;

	public SlugAtom() {
		// create animated sprite
		var animSprite = new AnimatedSprite2D();
		var frames = new SpriteFrames();
		
		frames.AddAnimation("slimeWalk");
		frames.SetAnimationSpeed("slimeWalk", 4); // 4 FPS
		frames.SetAnimationLoop("slimeWalk", true);
		
		// 33% chance of pink, green, or blue slug
		if (GD.Randf() < 0.33f) {
			frames.AddFrame("slimeWalk", GD.Load<Texture2D>("res://Assets/kenney_platformer-art-deluxe/Extra animations and enemies/Enemy sprites/slimeGreen.png"));
			frames.AddFrame("slimeWalk", GD.Load<Texture2D>("res://Assets/kenney_platformer-art-deluxe/Extra animations and enemies/Enemy sprites/slimeGreen_walk.png"));
		} else if (GD.Randf() < 0.67f) {
			frames.AddFrame("slimeWalk", GD.Load<Texture2D>("res://Assets/kenney_platformer-art-deluxe/Extra animations and enemies/Enemy sprites/slime.png"));
			frames.AddFrame("slimeWalk", GD.Load<Texture2D>("res://Assets/kenney_platformer-art-deluxe/Extra animations and enemies/Enemy sprites/slime_walk.png"));
		} else {
			frames.AddFrame("slimeWalk", GD.Load<Texture2D>("res://Assets/kenney_platformer-art-deluxe/Extra animations and enemies/Enemy sprites/slimeBlue.png"));
			frames.AddFrame("slimeWalk", GD.Load<Texture2D>("res://Assets/kenney_platformer-art-deluxe/Extra animations and enemies/Enemy sprites/slimeBlue_blue.png"));
		}

		animSprite.SpriteFrames = frames;
		animSprite.Play("slimeWalk");
		AddChild(animSprite);

		Size = new Vector2(60, 30);
		
		direction = GD.Randf() < 0.5f ? 1 : -1; // randomise initial direction
		Scale = new Vector2(-direction, 1); // flip based on direction
				
		// create collision
		CollisionShape2D collision = new CollisionShape2D();
		CapsuleShape2D shape = new CapsuleShape2D();
		shape.Radius = 15;
		shape.Height = 50; 
		collision.Shape = shape;
		collision.RotationDegrees = 90;
		collision.Position = new Vector2(-1, 1);
		AddChild(collision);

		AddToGroup("Slug");
		
		SetCollisionLayerValue(6, true);
		SetCollisionMaskValue(1, true);
		SetCollisionMaskValue(2, true);
		SetCollisionMaskValue(4, true);
	}

	// move slug and handle turning at edges or walls
	public override void _PhysicsProcess(double delta) {
		Vector2 pos = GlobalPosition;
		float rayLength = 50f;

		var spaceState = GetWorld2D().DirectSpaceState;

		// wall detection ray (horizontal)
		PhysicsRayQueryParameters2D wallRayParams = new PhysicsRayQueryParameters2D {
			From = pos,
			To = pos + new Vector2(direction * rayLength, 0),
			CollisionMask = (1 << 1) | (1 << 3), // collide with floors and blocks
			Exclude = new Godot.Collections.Array<Rid> { GetRid() }
		};

		var wallResult = spaceState.IntersectRay(wallRayParams);

		if (wallResult.Count > 0) {
			direction *= -1; // reverse on wall
			Scale = new Vector2(-Scale.X, Scale.Y);
		}

		// ground detection ray (downward from edge)
		float lookAheadDistance = (Size.X / 2f) + 6f;
		Vector2 downRayOrigin = pos + new Vector2(direction * lookAheadDistance, 0);
		Vector2 downRayEnd = downRayOrigin + new Vector2(0, rayLength);

		PhysicsRayQueryParameters2D groundRayParams = new PhysicsRayQueryParameters2D {
			From = downRayOrigin,
			To = downRayEnd,
			CollisionMask = (1 << 1), // floor layer only
			Exclude = new Godot.Collections.Array<Rid> { GetRid() }
		};

		var groundResult = spaceState.IntersectRay(groundRayParams);

		if (groundResult.Count == 0) {
			direction *= -1; // reverse at edge
			Scale = new Vector2(-Scale.X, Scale.Y);
		}

		// move slug in current direction
		pos.X += direction * speed * (float)delta;
		GlobalPosition = pos;
	}

	// allow placement anywhere for now
	public override bool ValidatePlacement(Room room) {
		return true;
	}
}

public partial class Slug : Primitive {
	
	public Slug() : base(Vector2.Zero) {	
		Category = PrimitiveCategory.Hazard;
		Difficulty = 3;
	}  // required for instantiation
	
	public Slug(Vector2 position) : base(position) {}

	// reverse direction of slug and flip sprite
	public void ChangeDirection () {
		SlugAtom slugAtom = GetAtoms().First() as SlugAtom;
		slugAtom.direction *= -1;
		slugAtom.Scale = new Vector2(-slugAtom.Scale.X, slugAtom.Scale.Y);
	}
	
	// spawn slug and apply floor difficulty bonus
	public override bool GenerateInRoom(Room room) {
		SlugAtom atom = new SlugAtom();
		atom.speed *= room.DifficultyPercent;
		atom.GlobalPosition = this.Position;
		AddAtom(atom);
		
		if (room.AddPrimitive(this)) {
			// locate floor underneath slug to increase its difficulty
			Floor floor = room.Primitives.FirstOrDefault(p => p.GetType() == typeof(Floor) 
				&& p.Position.Y == this.Position.Y + 70 
				&& p.Position.X <= this.Position.X 
				&& p.GetAtoms().OrderBy(a => a.GlobalPosition.X).Last().GlobalPosition.X >= this.Position.X) as Floor;
			
			if (floor != null) {
				floor.Difficulty += this.Difficulty;
			}
			return true;
		} 
		return false;
	}

	// no anchors needed for slugs
	public override void GenerateAnchors(Room room) { } 
}
