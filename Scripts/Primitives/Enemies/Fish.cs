using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class HorizontalFishAtom : Atom {
	
	private float speed = 40f;
	private int direction;
	public float leftBound;
	public float rightBound;

	public HorizontalFishAtom() {
		// Create animated sprite
		var animSprite = new AnimatedSprite2D();
		var frames = new SpriteFrames();
		
		frames.AddAnimation("horizontalFishSwim");
		frames.SetAnimationSpeed("horizontalFishSwim", 10); // 4 FPS
		frames.SetAnimationLoop("horizontalFishSwim", true);
		
		// 50% chance of choosing pink or green fish
		if (GD.Randf() < 0.5f) {
			frames.AddFrame("horizontalFishSwim", GD.Load<Texture2D>("res://Assets/kenney_platformer-art-deluxe/Extra animations and enemies/Enemy sprites/fishGreen.png"));
			frames.AddFrame("horizontalFishSwim", GD.Load<Texture2D>("res://Assets/kenney_platformer-art-deluxe/Extra animations and enemies/Enemy sprites/fishGreen_swim.png"));
		} else {
			frames.AddFrame("horizontalFishSwim", GD.Load<Texture2D>("res://Assets/kenney_platformer-art-deluxe/Extra animations and enemies/Enemy sprites/fishPink.png"));
			frames.AddFrame("horizontalFishSwim", GD.Load<Texture2D>("res://Assets/kenney_platformer-art-deluxe/Extra animations and enemies/Enemy sprites/fishPink_swim.png"));
		}

		animSprite.SpriteFrames = frames;
		animSprite.Play("horizontalFishSwim");
		
		AddChild(animSprite);

		Size = new Vector2(60, 30);
		
		direction = GD.Randf() < 0.5f ? 1 : -1;
		Scale = new Vector2(-direction, 1);
				
		CollisionShape2D collision = new CollisionShape2D();
		CapsuleShape2D shape = new CapsuleShape2D();
		shape.Radius = 15;
		shape.Height = 50; 
		collision.Shape = shape;
		collision.RotationDegrees = 90;
		collision.Position = new Vector2(-1, 1);

		AddChild(collision);
		AddToGroup("Fish");
		
		SetCollisionLayerValue(6, true);
		SetCollisionMaskValue(1, true);
	}

	public override void _PhysicsProcess(double delta) {
		Vector2 pos = GlobalPosition;
		pos.X += direction * speed * (float)delta;

		// Flip when hitting bounds
		if (pos.X < leftBound || pos.X > rightBound) {
			direction *= -1;
			
			// Flip sprite visually
			Scale = new Vector2(-Scale.X, Scale.Y);
		}

		GlobalPosition = pos;
	}

	public override bool ValidatePlacement(Room room) {
		// Ensure Mushroom is placed on a floor
		return true;
	}
}


public partial class VerticalFishAtom : Atom {
	
	private float speed = 40f;
	private int direction;
	public float upperBound;
	public float lowerBound;

	public VerticalFishAtom() {
		// Create animated sprite
		var animSprite = new AnimatedSprite2D();
		var frames = new SpriteFrames();
		
		frames.AddAnimation("verticalFishSwim");
		frames.SetAnimationSpeed("verticalFishSwim", 10); // 4 FPS
		frames.SetAnimationLoop("verticalFishSwim", true);
		
		frames.AddFrame("verticalFishSwim", GD.Load<Texture2D>("res://Assets/kenney_platformer-art-deluxe/Extra animations and enemies/Enemy sprites/piranha.png"));

		animSprite.SpriteFrames = frames;
		animSprite.Play("verticalFishSwim");
		
		AddChild(animSprite);

		Size = new Vector2(60, 30);
		
		direction = GD.Randf() < 0.5f ? 1 : -1;
		Scale = new Vector2(1, -direction);
				
		CollisionShape2D collision = new CollisionShape2D();
		CapsuleShape2D shape = new CapsuleShape2D();
		shape.Radius = 15;
		shape.Height = 50; 
		collision.Shape = shape;
		collision.Position = new Vector2(-1, 1);

		AddChild(collision);
		AddToGroup("Fish");
		
		SetCollisionLayerValue(6, true);
		SetCollisionMaskValue(1, true);
	}

	public override void _PhysicsProcess(double delta) {
		Vector2 pos = GlobalPosition;
		pos.Y += direction * speed * (float)delta;

		// Flip when hitting bounds
		if (pos.Y < upperBound || pos.Y > lowerBound) {
			direction *= -1;
			
			// Flip sprite visually
			Scale = new Vector2(Scale.X, -Scale.Y);
		}

		GlobalPosition = pos;
	}

	public override bool ValidatePlacement(Room room) {
		// Ensure Mushroom is placed on a floor
		return true;
	}
}


public partial class Fish : Primitive {
	
	public Fish() : base(Vector2.Zero) {	
		Category = PrimitiveCategory.Hazard;
	}  // Default constructor needed for instantiation
	
	public Fish(Vector2 position) : base(position) {}
	
	public override bool GenerateInRoom(Room room) {
		// Find water primitives
		List<Water> waterPrimitives = room.Primitives.Where(p => p is Water).Cast<Water>().OrderBy(_ => GD.Randf()).ToList();
		
		foreach (Water water in waterPrimitives) {
			
			if (water.availableFishPositions.Count() == 0) { continue; }
			
			if (water.Width < 5 && water.Depth > 5) {
				VerticalFishAtom atom = new VerticalFishAtom();
				atom.upperBound = water.Position.Y + 105;
				atom.lowerBound = water.Position.Y + ((water.Depth - 1) * 70);
				
				float y = (float)GD.RandRange((int)atom.upperBound + 35, (int)atom.lowerBound - 70);
				float x = water.availableFishPositions[0];
				water.availableFishPositions.RemoveAt(0);

				atom.GlobalPosition = new Vector2(x , y);			

				AddAtom(atom);
				this.Position = atom.GlobalPosition;
				return room.AddPrimitive(this);
				
			} else if (water.Width >= 5) {
				HorizontalFishAtom atom = new HorizontalFishAtom();
				atom.leftBound = water.Position.X + 70;
				atom.rightBound = water.Position.X + ((water.Width - 1) * 70);
				
				float y = water.availableFishPositions[0];
				water.availableFishPositions.RemoveAt(0);
				
				float x = (float)GD.RandRange((int)atom.leftBound + 35, (int)atom.rightBound - 35);

				atom.GlobalPosition = new Vector2(x , y);			

				AddAtom(atom);
				this.Position = atom.GlobalPosition;
				return room.AddPrimitive(this);
			}
		}

		GD.Print("❌ No suitable water area found for fish.");
		return false;
	}
	
	public override void GenerateAnchors(Room room) { } 
}
