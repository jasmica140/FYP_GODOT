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
			typeof(Platform),
			typeof(Slope),
			typeof(FloorBlade),
			typeof(Water)
			// Add more primitives here
		};
	}
}

public abstract partial class Primitive : StaticBody2D
{
	protected Sprite2D sprite;

	public Vector2 Position { get; set; }
	
	// Define categories
	public enum PrimitiveCategory { None, Hazard, Collectible, Platform, Obstacle, Environmental, MovementModifier, Floor, Test }
	public PrimitiveCategory Category { get; protected set; } = PrimitiveCategory.None;

	// Each primitive contains a list of atoms
	protected List<Atom> atoms = new List<Atom>();
	//protected List <Anchor> anochor = new List<anchor>();

	public Primitive(Vector2 position) {
		Position = position;
		sprite = new Sprite2D();
		AddChild(sprite);
		//sprite.Scale = new Vector2(3, 3); // Scale up by 3x
		//sprite.Position += new Vector2(16, 16); // Offset to prevent stacking
	}
	
	// helper to add an atom and track it
	public void AddAtom(Atom atom) {
		atoms.Add(atom);
		AddChild(atom);
	}
	
	public List<Atom> GetAtoms() {
		return atoms;
	}	
	
	public void ReplaceAtom(Atom oldAtom, Atom newAtom) {
		if (atoms.Contains(oldAtom)) {
			int index = atoms.IndexOf(oldAtom); 
			atoms[index] = newAtom; // Replace in internal list
			oldAtom.QueueFree(); // Remove from scene
			AddChild(newAtom); // Add to scene
			newAtom.GlobalPosition = oldAtom.GlobalPosition; // Copy position
			GD.Print($"🔄 Replaced {oldAtom.GetType().Name} with {newAtom.GetType().Name} at {newAtom.GlobalPosition}");
		} else {
			GD.PrintErr("❌ Tried to replace an atom not part of this primitive.");
		}
	}
	
	public void ReplaceAtomAt(Vector2 position, Atom newAtom) {
		Atom target = atoms.Find(a => a.GlobalPosition == position);
		if (target != null) {
			ReplaceAtom(target, newAtom);
		}
	}

	// Every Primitive must define how it generates itself in a Room
	public abstract void GenerateInRoom(Room room);
}
