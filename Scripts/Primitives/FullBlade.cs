using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class FullBladeAtom : Atom {
	public FullBladeAtom() {
		// Create animated sprite
		var animSprite = new AnimatedSprite2D();
		var frames = new SpriteFrames();
		
		frames.AddAnimation("spin");
		frames.SetAnimationSpeed("spin", 10); // 4 FPS
		frames.SetAnimationLoop("spin", true);
		
		frames.AddFrame("spin", GD.Load<Texture2D>("res://Assets/kenney_platformer-art-deluxe/Extra animations and enemies/Enemy sprites/spinner.png"));
		frames.AddFrame("spin", GD.Load<Texture2D>("res://Assets/kenney_platformer-art-deluxe/Extra animations and enemies/Enemy sprites/spinner_spin.png"));

		animSprite.SpriteFrames = frames;
		animSprite.Play("spin");

		AddChild(animSprite);

		Size = new Vector2(70, 70);
		
		CollisionShape2D collision = new CollisionShape2D();
		CircleShape2D shape = new CircleShape2D();
		shape.Radius = 30;
		collision.Shape = shape;

		AddChild(collision);		
		AddToGroup("FullBlade");

		SetCollisionLayerValue(6, true);
		SetCollisionMaskValue(1, true);
	}
	
	public override bool ValidatePlacement(Room room) {
		// Ensure Mushroom is placed on a floor
		return true;
	}
}

public partial class FullBlade : Primitive
{
	public FullBlade() : base(Vector2.Zero) {	
		Category = PrimitiveCategory.Hazard;
	}  // Default constructor needed for instantiation
	
	public FullBlade(Vector2 position) : base(position) {}
	
	
	public override bool GenerateInRoom(Room room) {
		this.Position -= new Vector2(0, 70); // put blade one tile higher
		FullBladeAtom atom = new FullBladeAtom();
		atom.GlobalPosition = this.Position;
		AddAtom(atom);
		
		return room.AddPrimitive(this);
	}
	
	public override void GenerateAnchors(Room room)
	{
		GenerateObstructionLines();
	}
	
	public void GenerateObstructionLines()
	{
		ObstructionLines.Clear();
		
		List<Atom> atoms = GetAtoms(); // This should return the ladder tiles
		Atom atom = atoms.First();
		
		Vector2 topLeft = atom.GlobalPosition + new Vector2((-atom.Size.X / 2), -(atom.Size.Y / 2));
		Vector2 topRight = atom.GlobalPosition + new Vector2((atom.Size.X / 2), -(atom.Size.Y / 2));
		Vector2 bottomLeft = atom.GlobalPosition + new Vector2((-atom.Size.X / 2), (atom.Size.Y / 2));
		Vector2 bottomRight = atom.GlobalPosition + new Vector2((atom.Size.X / 2), (atom.Size.Y / 2));

		ObstructionLines.Add((topLeft, topRight));
		ObstructionLines.Add((topLeft, bottomLeft));
		ObstructionLines.Add((topRight, bottomRight));
		ObstructionLines.Add((bottomLeft, bottomRight));

	}
}
