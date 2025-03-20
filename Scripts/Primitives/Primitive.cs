using Godot;
using System;
using System.Collections.Generic;

public static class PrimitiveRegistry {
	public static List<Type> GetAllPrimitives() {
		return new List<Type> {
			typeof(Floor),
			typeof(Cactus),
			typeof(Ladder),
			typeof(Mushroom),
			typeof(Platform)
			// Add more primitives here
		};
	}
}

public abstract partial class Primitive : StaticBody2D
{
	protected Sprite2D sprite;

	public Vector2 Position { get; set; }
	
	// Define categories
	public enum PrimitiveCategory { None, Hazard, Collectible, Platform, Obstacle, Environmental, MovementModifier, Floor }
	public PrimitiveCategory Category { get; protected set; } = PrimitiveCategory.None;

	// Each primitive contains a list of atoms
	protected List<Atom> atoms = new List<Atom>();

	public Primitive(Vector2 position) {
		Position = position;
		sprite = new Sprite2D();
		AddChild(sprite);
		//sprite.Scale = new Vector2(3, 3); // Scale up by 3x
		//sprite.Position += new Vector2(16, 16); // Offset to prevent stacking
	}
	
	public List<Atom> GetAtoms() {
		return atoms;
	}	

	// Every Primitive must define how it generates itself in a Room
	public abstract void GenerateInRoom(Room room);
}
