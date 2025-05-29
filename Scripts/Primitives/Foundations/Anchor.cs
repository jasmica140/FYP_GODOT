using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class AnchorConnection
{
	public Anchor From { get; }
	public Anchor To { get; }
	public bool IsBidirectional { get; }

	// sets up connection between two anchors
	public AnchorConnection(Anchor from, Anchor to, bool isBidirectional = true)
	{
		From = from;
		To = to;
		IsBidirectional = isBidirectional;
	}
	
	// checks if any obstruction line crosses the connection
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
	
	// basic line intersection check
	public static bool DoLinesIntersect(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2)
	{
		float d = (p2.X - p1.X) * (q2.Y - q1.Y) - (p2.Y - p1.Y) * (q2.X - q1.X);
		if (d == 0) return false; // parallel

		float u = ((q1.X - p1.X) * (q2.Y - q1.Y) - (q1.Y - p1.Y) * (q2.X - q1.X)) / d;
		float v = ((q1.X - p1.X) * (p2.Y - p1.Y) - (q1.Y - p1.Y) * (p2.X - p1.X)) / d;

		return (u >= 0 && u <= 1 && v >= 0 && v <= 1);
	}
}

public class Anchor
{
	public Vector2 Position { get; private set; } // global pos
	public float Radius { get; private set; }     // orbit radius
	public string Type { get; private set; }      // like top, bottom, jump_arc, etc
	public Primitive Owner { get; private set; }

	// creates anchor
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

	// checks if two anchors are close enough to connect
	public bool IsConnectedTo(Anchor other)
	{
		return Position.DistanceTo(other.Position) <= (Radius + other.Radius);
	}
	
	// picks random point in orbit
	public Vector2 GetRandomNearbyPoint()
	{
		Random rand = new Random();
		double angle = rand.NextDouble() * 2 * Math.PI;
		float dx = Radius * Mathf.Cos((float)angle);
		float dy = Radius * Mathf.Sin((float)angle);
		return Position + new Vector2(dx, dy);
	}
	
	// redraw anchor in editor
	public void DebugDraw()
	{
		if (Owner is Node2D node)
		{
			node.QueueRedraw();
		}
	}
}

public static class AnchorConnector
{
	// tries to grow room by adding new primitives near anchors
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
	
	// tries to connect every anchor to every other anchor
	public static void ConnectAnchors(Room room)
	{
		var allAnchors = room.GetAllAnchors();

		for (int i = 0; i < allAnchors.Count; i++)
		{
			for (int j = i + 1; j < allAnchors.Count; j++)
			{
				Anchor a1 = allAnchors[i];
				Anchor a2 = allAnchors[j];

				if (a1.IsConnectedTo(a2))
				{
					// debug print goes here if needed
				}
			}
		}
	}

	// picks random primitive type using weighted probs
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
	
	// removes internal anchor paths if another primitive blocks them
	public static void RemoveIntersectingAnchorConnections(Room room)
	{
		foreach (Primitive primitive in room.Primitives)
		{
			if (primitive is Pit || primitive is Water) continue;
			
			List<AnchorConnection> toRemove = new List<AnchorConnection>();

			foreach (AnchorConnection connection in primitive.InternalPaths)
			{
				foreach (Primitive other in room.Primitives)
				{
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
	
	// fancy line intersection check with collinearity handling
	public static bool DoLinesIntersect(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2)
	{
		// skip if endpoint lies directly on obstruction
		if (PointOnSegment(b1, b2, a1) || PointOnSegment(b1, b2, a2))
			return false;

		int Orientation(Vector2 p, Vector2 q, Vector2 r)
		{
			float val = (q.Y - p.Y) * (r.X - q.X) - (q.X - p.X) * (r.Y - q.Y);
			if (Mathf.Abs(val) < 0.00001f) return 0; // collinear
			return (val > 0) ? 1 : 2;
		}

		bool PointOnSegment(Vector2 p, Vector2 r, Vector2 q)
		{
			return q.X <= Mathf.Max(p.X, r.X) && q.X >= Mathf.Min(p.X, r.X) &&
				   q.Y <= Mathf.Max(p.Y, r.Y) && q.Y >= Mathf.Min(p.Y, r.Y) &&
				   Orientation(p, r, q) == 0;
		}

		int o1 = Orientation(a1, a2, b1);
		int o2 = Orientation(a1, a2, b2);
		int o3 = Orientation(b1, b2, a1);
		int o4 = Orientation(b1, b2, a2);

		if (o1 != o2 && o3 != o4)
			return true;

		// collinear + overlapping
		if (o1 == 0 && PointOnSegment(a1, a2, b1)) return true;
		if (o2 == 0 && PointOnSegment(a1, a2, b2)) return true;
		if (o3 == 0 && PointOnSegment(b1, b2, a1)) return true;
		if (o4 == 0 && PointOnSegment(b1, b2, a2)) return true;

		return false;
	}
}
