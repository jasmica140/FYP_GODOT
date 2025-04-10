using Godot;
using System;
using System.Collections.Generic;

public class PathBuilder
{
	private Room room;
	private Random rng = new Random();

	public PathBuilder(Room room)
	{
		this.room = room;
	}

	public bool GeneratePath(Anchor start, Anchor end)
	{
		GD.Print($"🚶 Starting path from {start.Type} at {start.Position} to {end.Type} at {end.Position}");

		Anchor current = start;
		int maxSteps = 20;
		int steps = 0;

		while (steps < maxSteps)
		{
			// Step 1: Find all anchors currently in the room that intersect with the current one
			Anchor nextAnchor = FindNextAnchor(current, end);

			if (nextAnchor == null)
			{
				GD.Print("⚠️ No valid next anchor found. Path generation failed.");
				return false;
			}

			// Step 2: Connect the current anchor to the next anchor
			GD.Print($"🔗 Step {steps + 1}: Connecting {current.Type} to {nextAnchor.Type}.");
			DrawDebugLine(current.Position, nextAnchor.Position);

			if (nextAnchor == end)
			{
				GD.Print("✅ Path successfully connected to exit!");
				return true;
			}

			current = nextAnchor;
			steps++;
		}

		GD.Print("❌ Max steps reached. Path generation failed.");
		return false;
	}

	private Anchor FindNextAnchor(Anchor current, Anchor target)
	{
		List<Anchor> allAnchors = room.GetAllAnchors();
		Anchor best = null;
		float bestScore = float.MaxValue;

		// 1. Try to find a direct connection from current to any existing anchor
		foreach (Anchor candidate in allAnchors)
		{
			if (candidate == current) continue;

			if (current.IsConnectedTo(candidate))
			{
				float score = candidate.Position.DistanceTo(target.Position);
				if (score < bestScore)
				{
					best = candidate;
					bestScore = score;
				}
			}
		}

		if (best != null)
			return best;

		// 2. No connection found — try to place a new primitive
		GD.Print("➕ Attempting to place a bridging primitive...");

		// Get compatible types from matrix
		Dictionary<Type, float> compatibleTypes = CompatibilityMatrix.GetCompatibleTypes(current.Owner.GetType()); // You need to set OwnerType when creating anchors

		foreach (KeyValuePair<Type, float> entry in compatibleTypes) {
			Type type = entry.Key;
			float probability = entry.Value;
	
			// Create a new primitive of that type
			Primitive newPrimitive = (Primitive)Activator.CreateInstance(type);
			newPrimitive.Position = current.Position + new Vector2(50, 0); // Offset position near anchor

			newPrimitive.GenerateInRoom(room); // Attempt to add it to the room
			newPrimitive.GenerateAnchors();

			foreach (Anchor a in newPrimitive.Anchors)
			{
				if (current.IsConnectedTo(a))
				{
					GD.Print($"✅ Placed {type.Name} near anchor. New anchor found.");
					return a;
				}
			}

			// If anchor didn’t connect, remove it
			room.RemovePrimitive(newPrimitive);
		}

		return null; // Failed to find or place a connector
	}

	private void DrawDebugLine(Vector2 from, Vector2 to)
	{
		var line = new Line2D();
		line.Width = 2;
		line.DefaultColor = Colors.Red;
		line.AddPoint(from);
		line.AddPoint(to);
		room.AddChild(line); // Add the line to the room for visualization
	}
}
