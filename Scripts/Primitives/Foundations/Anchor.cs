using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class Anchor
{
	public Vector2 Position { get; private set; } // World/global position
	public float Radius { get; private set; }     // The orbit radius
	public string Type { get; private set; }      // e.g., "top", "bottom", "jump_arc", etc.
	public Primitive Owner { get; private set; }

	public Anchor(Vector2 position, float radius, string type, Primitive owner = null)
	{
		Position = position;
		Radius = radius;
		Type = type;
		Owner = owner;
	}

	// Check if another anchor is close enough to connect
	public bool IsConnectedTo(Anchor other)
	{
		return Position.DistanceTo(other.Position) <= (Radius + other.Radius);
	}
	
	public Vector2 GetRandomNearbyPoint()
	{
		Random rand = new Random();
		double angle = rand.NextDouble() * 2 * Math.PI;
		float dx = Radius * Mathf.Cos((float)angle);
		float dy = Radius * Mathf.Sin((float)angle);
		return Position + new Vector2(dx, dy);
	}
	
	public void DebugDraw()
	{
		if (Owner is Node2D node)
		{
			node.QueueRedraw(); // ask Godot to call _Draw
		}
	}
}

public static class AnchorConnector
{
	public static void ExpandRoomFromAnchors(Room room, int expansionLimit = 10)
	{
		int expansions = 0;
		Random rng = new Random();

		foreach (Primitive origin in room.Primitives.ToList())
		{
			foreach (Anchor anchor in origin.Anchors)
			{
				var compatibleTypes = CompatibilityMatrix.GetCompatibleTypes(origin.GetType());
				if (compatibleTypes.Count == 0)
					continue;

				Type selected = GetRandomWeightedPrimitive(origin.GetType(), compatibleTypes, rng);
				if (selected == null)
					continue;

				Vector2 spawnPos = anchor.GetRandomNearbyPoint();

				if (Activator.CreateInstance(selected) is Primitive newPrimitive)
				{
					newPrimitive.GlobalPosition = spawnPos;
					newPrimitive.GenerateAnchors();
					room.AddPrimitive(newPrimitive);

					expansions++;
					if (expansions >= expansionLimit)
						return;
				}
			}
		}
	}
	
	public static void ConnectAnchors(Room room)
	{
		var allAnchors = room.GetAllAnchors(); // however you're storing them

		for (int i = 0; i < allAnchors.Count; i++)
		{
			for (int j = i + 1; j < allAnchors.Count; j++)
			{
				Anchor a1 = allAnchors[i];
				Anchor a2 = allAnchors[j];

				GD.Print($"🧲 Checking anchor from {a1.Owner.GetType().Name} ({a1.Type}) at {a1.Position} " +
						 $"to {a2.Owner.GetType().Name} ({a2.Type}) at {a2.Position}");

				if (a1.IsConnectedTo(a2))
				{
					GD.Print($"✅ Anchors CONNECTED: {a1.Owner.GetType().Name} → {a2.Owner.GetType().Name}");
					// Optionally place a connector primitive or visual
				}
			}
		}
	}

	private static Type GetRandomWeightedPrimitive(Type from, Dictionary<Type, float> compatibleTypes, Random rng)
	{
		float total = compatibleTypes.Values.Sum();
		float roll = (float)(rng.NextDouble() * total);

		foreach (var pair in compatibleTypes)
		{
			roll -= pair.Value;
			if (roll <= 0)
				return pair.Key;
		}

		return null;
	}
}
