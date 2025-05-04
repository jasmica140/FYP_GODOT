using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class HorizontalFishAtom : Atom {
	public HorizontalFishAtom() {
		// Create animated sprite
		var animSprite = new AnimatedSprite2D();
		var frames = new SpriteFrames();
		
		frames.AddAnimation("horizontalFishSwim");
		frames.SetAnimationSpeed("horizontalFishSwim", 10); // 4 FPS
		frames.SetAnimationLoop("horizontalFishSwim", true);
		
		frames.AddFrame("horizontalFishSwim", GD.Load<Texture2D>("res://Assets/kenney_platformer-art-deluxe/Extra animations and enemies/Enemy sprites/fishGreen.png"));
		frames.AddFrame("horizontalFishSwim", GD.Load<Texture2D>("res://Assets/kenney_platformer-art-deluxe/Extra animations and enemies/Enemy sprites/fishGreen_swim.png"));

		animSprite.SpriteFrames = frames;
		animSprite.Play("horizontalFishSwim");

		AddChild(animSprite);

		Size = new Vector2(60, 30);

		CollisionShape2D collision = new CollisionShape2D();
		CapsuleShape2D shape = new CapsuleShape2D();
		shape.Radius = 6;
		shape.Height = 38; 
		collision.Shape = shape;
		collision.RotationDegrees = 90;
		collision.Position = new Vector2(-1, 1);

		AddChild(collision);
		AddToGroup("Fish");
		
		SetCollisionLayerValue(2, true);
		SetCollisionMaskValue(1, true);
	}
	
	public override bool ValidatePlacement(Room room) {
		// Ensure Mushroom is placed on a floor
		return true;
	}
}

public partial class HorizontalFish : Primitive {
	
	public HorizontalFish() : base(Vector2.Zero) {	
		Category = PrimitiveCategory.Hazard;
	}  // Default constructor needed for instantiation
	
	public HorizontalFish(Vector2 position) : base(position) {}
	
	public override bool GenerateInRoom(Room room) {
		// Find water primitives
		List<Water> waterPrimitives = room.Primitives.Where(p => p is Water).Cast<Water>().ToList();
		
		foreach (Water water in waterPrimitives) {
			if (water.hasFish || water.Width < 5) { continue; }
			
			HorizontalFishAtom atom = new HorizontalFishAtom();
			atom.GlobalPosition = water.Position + new Vector2((water.Width - 1) * 70, 35);
			AddAtom(atom);
			return room.AddPrimitive(this); // Only add one fish per matching water
		}

		GD.Print("❌ No suitable water area found for fish.");
		return false;
	}
	
	public override void GenerateAnchors(Room room) { } 
}
