using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class AnchorConnection
{
	public Anchor From { get; }
	public Anchor To { get; }
	public bool IsBidirectional { get; }

	public AnchorConnection(Anchor from, Anchor to, bool isBidirectional = true)
	{
		From = from;
		To = to;
		IsBidirectional = isBidirectional;
	}
	
	public bool IsConnectionObstructed(Room room)
	{
		foreach (Primitive p in room.Primitives)
		{
			foreach (var line in p.ObstructionLines)
			{
				if (DoLinesIntersect(this.From.Position, this.To.Position, line.start, line.end))
					return true;
			}
		}
		return false;
	}
	
	public static bool DoLinesIntersect(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2)
	{
		float d = (p2.X - p1.X) * (q2.Y - q1.Y) - (p2.Y - p1.Y) * (q2.X - q1.X);
		if (d == 0) return false; // parallel lines

		float u = ((q1.X - p1.X) * (q2.Y - q1.Y) - (q1.Y - p1.Y) * (q2.X - q1.X)) / d;
		float v = ((q1.X - p1.X) * (p2.Y - p1.Y) - (q1.Y - p1.Y) * (p2.X - p1.X)) / d;

		return (u >= 0 && u <= 1 && v >= 0 && v <= 1);
	}
}

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
	
	public override bool Equals(object obj)
	{
		if (obj is Anchor other)
		{
			return Position == other.Position && Type == other.Type;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Position, Type);
	}

	public override string ToString()
	{
		return $"Anchor({Position}, {Type})";
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
					newPrimitive.GenerateAnchors(room);
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
	
	public static void RemoveIntersectingAnchorConnections(Room room)
	{
		foreach (Primitive primitive in room.Primitives)
		{
			List<AnchorConnection> toRemove = new List<AnchorConnection>();

			foreach (AnchorConnection connection in primitive.InternalPaths)
			{
				foreach (Primitive other in room.Primitives)
				{
					// Skip obstruction checks from the same primitive
					if (other == primitive)
						continue;

					foreach (var obstruction in other.ObstructionLines)
					{
						if (DoLinesIntersect(connection.From.Position, connection.To.Position, obstruction.start, obstruction.end))
						{
							toRemove.Add(connection);
							break;
						}
					}
				}
			}

			foreach (var connection in toRemove)
			{
				primitive.InternalPaths.Remove(connection);
			}
		}
	}
	
	public static bool DoLinesIntersect(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2)
	{
		float ccw(Vector2 A, Vector2 B, Vector2 C) => (C.Y - A.Y) * (B.X - A.X) - (B.Y - A.Y) * (C.X - A.X);
		return (ccw(a1, b1, b2) * ccw(a2, b1, b2) < 0) && (ccw(b1, a1, a2) * ccw(b2, a1, a2) < 0);
	}
}
