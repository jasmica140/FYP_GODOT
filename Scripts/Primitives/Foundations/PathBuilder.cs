using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class PathBuilder
{
	private Room room;
	private Dictionary<Anchor, List<Anchor>> graph = new();

	private Random rng = new Random();

	public PathBuilder(Room room)
	{
		this.room = room;
		//BuildGraph();
	}
	
	public void GenerateHazards()
	{
		List<Vector2> validPositions = room.GetPositionsAboveFloorTiles();
		Random random = new Random();
		int noOfHazards = random.Next(2, 10); // random number of hazards to place

		// Manually list available hazard types
		List<Type> hazardTypes = new List<Type> { typeof(FloorBlade) }; // Add more hazard types here

		for (int i = 0; i < noOfHazards; i++)
		{
			if (validPositions.Count == 0)
			{
				GD.Print("⚠️ No more valid positions left for hazards.");
				break;
			}

			int index = random.Next(validPositions.Count);
			Vector2 chosenPosition = validPositions[index];
			validPositions.RemoveAt(index); // remove the position immediately

			// Pick a random hazard type
			Type hazardType = hazardTypes[random.Next(hazardTypes.Count)];
			Primitive hazard = (Primitive)Activator.CreateInstance(hazardType);
			hazard.Position = chosenPosition;

			// Try to place it
			if (!hazard.GenerateInRoom(room))
			{
				GD.Print($"❌ Failed to place {hazardType.Name} at {chosenPosition}. Trying another...");
				i--; // Try again (retry this iteration)
			}
		}
	}
	
	public void BuildPathsBetweenDoors(Room room)
	{
		GD.Print("🔗 Building anchor graph...");
		BuildGraph(room.Primitives); // Build the full anchor graph
		
		foreach (var anchor in graph.Keys)
		{
			if (anchor.Owner is Door)
			{
				GD.Print($"🚪 Door anchor at {anchor.Position} has {graph[anchor].Count} connections.");
				foreach (var connected in graph[anchor])
					GD.Print($"     ↳ Connected to {connected.Position} ({connected.Owner.GetType().Name})");
			}
		}
		
		// Get bottom-center anchors from doors
		List<Anchor> doorAnchors = room.Primitives
			.Where(p => p is Door)
			.SelectMany(p => p.Anchors)
			.Where(a => a.Type == "center") // or whatever you named it
			.ToList();

		GD.Print($"🚪 Found {doorAnchors.Count} door anchors");

		// Loop through all unique pairs
		for (int i = 0; i < doorAnchors.Count; i++)
		{
			for (int j = i + 1; j < doorAnchors.Count; j++)
			{
				Anchor start = doorAnchors[i];
				Anchor end = doorAnchors[j];

				List<Anchor> path = FindPath(start, end);

				if (path.Count > 0)
				{
					GD.Print($"✅ Path found between Door {i} and Door {j}");
					DrawPath(path, room); // Optional for visualization
				}
				else
				{
					GD.Print($"❌ No path found between Door {i} and Door {j}");
				}
			}
		}
	}
	
public void BuildGraph(List<Primitive> primitives)
{
	graph.Clear();
	List<Anchor> allAnchors = new List<Anchor>();

	// Step 0: Gather all anchors and add them to the graph with empty lists
	foreach (var primitive in primitives)
	{
		foreach (var anchor in primitive.Anchors)
		{
			if (!graph.ContainsKey(anchor))
				graph[anchor] = new List<Anchor>();
		}

		allAnchors.AddRange(primitive.Anchors);
	}

	// Step 1: Add internal connections
	foreach (var primitive in primitives)
	{
		foreach (var connection in primitive.InternalPaths)
		{
			graph[connection.From].Add(connection.To);

			if (connection.IsBidirectional)
			{
				graph[connection.To].Add(connection.From);
			}
		}
	}

	// Step 2: Connect intersecting orbits between anchors from different primitives
	for (int i = 0; i < allAnchors.Count; i++)
	{
		for (int j = i + 1; j < allAnchors.Count; j++)
		{
			Anchor a = allAnchors[i];
			Anchor b = allAnchors[j];

			if ((a.Position - b.Position).Length() <= (a.Radius + b.Radius))
			{
				graph[a].Add(b);
				graph[b].Add(a);
			}
		}
	}

	GD.Print($"✅ Anchor graph built with {graph.Count} nodes.");
}
		
	// BFS to find path between two anchors
	public List<Anchor> FindPath(Anchor start, Anchor goal) {
		
		GD.Print($"🔍 Starting pathfinding from {start.Position} to {goal.Position}");

		Queue<List<Anchor>> queue = new();
		HashSet<Anchor> visited = new();

		queue.Enqueue(new List<Anchor> { start });
		visited.Add(start);

		int iteration = 0;

		while (queue.Count > 0)
		{
			List<Anchor> path = queue.Dequeue();
			Anchor current = path.Last();

			//GD.Print($"🔁 Iteration {++iteration} | Visiting: {current.Position} | Path length: {path.Count}");

			if (current == goal)
			{
				GD.Print($"✅ Path found! Total steps: {path.Count}");
				return path;
			}

			if (!graph.ContainsKey(current))
			{
				GD.Print($"⚠️ Current anchor {current.Position} not found in graph.");
				continue;
			}

			foreach (var neighbor in graph[current])
			{
				if (!visited.Contains(neighbor))
				{
					//GD.Print($"➡️ Exploring neighbor: {neighbor.Position}");
					visited.Add(neighbor);
					var newPath = new List<Anchor>(path) { neighbor };
					queue.Enqueue(newPath);
				}
				else
				{
					//GD.Print($"⛔ Already visited: {neighbor.Position}");
				}
			}
		}

		GD.PrintErr($"❌ No path found from {start.Position} to {goal.Position}");
		return null;
	}

private void DrawPath(List<Anchor> path, Room room)
{
	for (int i = 0; i < path.Count - 1; i++)
	{
		Vector2 from = path[i].Position;
		Vector2 to = path[i + 1].Position;

		room.DebugPathLines.Add((from, to));
	}
	room.QueueRedraw();
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
			newPrimitive.GenerateAnchors(room);

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
