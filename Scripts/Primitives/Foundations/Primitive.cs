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
			typeof(Water),
			typeof(StickyFloor),
			typeof(SlipperyFloor),
			typeof(Door)

			// Add more primitives here
		};
	}
}

public abstract partial class Primitive : StaticBody2D
{
	protected Sprite2D sprite;

	public Vector2 Position { get; set; }
	
	// Define categories
	public enum PrimitiveCategory { None, Hazard, Collectible, Platform, Obstacle, Environmental, MovementModifier, Floor, Exit, Test }
	public PrimitiveCategory Category { get; protected set; } = PrimitiveCategory.None;

	// Each primitive contains a list of atoms
	protected List<Atom> atoms = new List<Atom>();
	public List<Anchor> Anchors { get; protected set; } = new();

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
	
	public void RemoveAtom(Atom atom) {
		if (atoms.Contains(atom)) {
			atoms.Remove(atom);         // Remove from the list
			atom.QueueFree();           // Remove from the scene
			GD.Print($"❌ Removed {atom.GetType().Name} at {atom.GlobalPosition} from {GetType().Name}");
		} else {
			GD.Print($"⚠️ Atom not found in {GetType().Name}'s atom list.");
		}
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
	public abstract void GenerateAnchors();
	
	public override void _Draw()
	{
		foreach (Anchor anchor in Anchors)
		{
			// Draw orbit
			DrawCircle(ToLocal(anchor.Position), anchor.Radius, new Color(1, 1, 0, 0.3f)); // yellow transparent

			// Draw point
			DrawCircle(ToLocal(anchor.Position), 4, new Color(1, 0, 0)); // red center
		}

		// Optional: Draw lines between your own anchors (for testing)
		for (int i = 0; i < Anchors.Count - 1; i++)
		{
			DrawLine(ToLocal(Anchors[i].Position), ToLocal(Anchors[i + 1].Position), Colors.Green, 2);
		}
	}
}
