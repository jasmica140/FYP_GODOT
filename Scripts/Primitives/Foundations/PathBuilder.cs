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
		List<Type> hazardTypes = new List<Type> { typeof(FloorBlade), typeof(FullBlade), typeof(Fish), typeof(Slug) };
		List<Vector2> validPositions = room.GetPositionsAboveFloorTiles();

		// Scale min and max with difficulty
		Random random = new Random();
		int minHazards = (int)MathF.Floor(15 * room.DifficultyPercent);
		int maxHazards = (int)MathF.Ceiling(20 * room.DifficultyPercent);
		int noOfHazards = random.Next(minHazards, maxHazards);

		for (int i = 0; i < noOfHazards; i++) {
			if (hazardTypes.Count == 0) {
				GD.Print("⚠️ No hazard types remaining.");
				break;
			}

			if (validPositions.Count == 0) {
				GD.Print("⚠️ No more valid positions left for hazards.");
				break;
			}

			// Pick a hazard type
			Type hazardType = hazardTypes[random.Next(hazardTypes.Count)];

			if (hazardType == typeof(Fish)) {
				Fish fish = new Fish();
				if (!fish.GenerateInRoom(room)) { 
					validPositions = room.GetPositionsAboveFloorTiles();
					GD.Print("🚫 No valid spot for pit. Removing it from hazard list.");
					hazardTypes.Remove(hazardType);
					i--; // Retry the current iteration
				} 
				continue; // Skip the rest of the loop for fish
			} 

			// For non-pit hazards
			int index = random.Next(validPositions.Count);
			Vector2 chosenPosition = validPositions[index];
			validPositions.RemoveAt(index);

			Primitive hazard = (Primitive)Activator.CreateInstance(hazardType);
			hazard.Position = chosenPosition;

			if (!hazard.GenerateInRoom(room)) {
				GD.Print($"❌ Failed to place {hazardType.Name} at {chosenPosition}. Trying another...");
				i--; // Retry
			}
		}
	}
	
	public void GenerateEnvironmentals()
	{
		List<Type> environmentalTypes = new List<Type> { typeof(Water), typeof(Pit) };
		List<Vector2> validPositions = room.GetPositionsAboveFloorTiles();

		// Scale min and max with difficulty
		Random random = new Random();
		//int minHazards = (int)MathF.Floor(15 * room.DifficultyPercent);
		//int maxHazards = (int)MathF.Ceiling(20 * room.DifficultyPercent);
		int noOfEnv = random.Next(1, 5);

		for (int i = 0; i < noOfEnv; i++) {
			if (environmentalTypes.Count == 0) {
				GD.Print("⚠️ No hazard types remaining.");
				break;
			}

			if (validPositions.Count == 0) {
				GD.Print("⚠️ No more valid positions left for hazards.");
				break;
			}

			// Pick a hazard type
			Type environmentalType = environmentalTypes[random.Next(environmentalTypes.Count)];

			if (environmentalType == typeof(Water)) {
				Water water = new Water();

				// Let Pit find its own valid placement
				bool success = water.GenerateInRoom(room);

				if (success) {
					// Recompute valid floor tile positions after modifying the room
					validPositions = room.GetPositionsAboveFloorTiles();
				} else {
					GD.Print("🚫 No valid spot for water. Removing it from env list.");
					environmentalTypes.Remove(environmentalType);
					i--; // Retry the current iteration
				}

				continue; // Skip the rest of the loop for pit
			} else if (environmentalType == typeof(Pit)) {
				Pit pit = new Pit();

				if (pit.GenerateInRoom(room)) { // Recompute valid floor tile positions after modifying the room
					validPositions = room.GetPositionsAboveFloorTiles();
				} else {
					GD.Print("🚫 No valid spot for pit. Removing it from env list.");
					environmentalTypes.Remove(environmentalType);
					i--; // Retry the current iteration
				}

				continue; // Skip the rest of the loop for pit
			}

			// For non-pit hazards
			int index = random.Next(validPositions.Count);
			Vector2 chosenPosition = validPositions[index];
			validPositions.RemoveAt(index);

			Primitive environmental = (Primitive)Activator.CreateInstance(environmentalType);
			environmental.Position = chosenPosition;

			if (!environmental.GenerateInRoom(room)) {
				GD.Print($"❌ Failed to place {environmentalType.Name} at {chosenPosition}. Trying another...");
				i--; // Retry
			}
		}
	}
	
	
	public void BuildPathsBetweenDoors(Room room)
	{
		GD.Print("🔗 Building anchor graph...");
		BuildGraph(room.Primitives); // Build the full anchor graph

		var doorAnchors = room.Primitives
			.Where(p => p is Door)
			.SelectMany(p => p.Anchors)
			.Where(a => a.Type == "center")
			.ToList();

		var collectibleAnchors = room.Primitives
			.Where(p => p.Category == Primitive.PrimitiveCategory.Collectible)
			.SelectMany(p => p.Anchors)
			.Where(a => a.Type == "center")
			.ToList();

		GD.Print($"🚪 Found {doorAnchors.Count} door anchors");
		GD.Print($"🎁 Found {collectibleAnchors.Count} collectible anchors");

		// Log connections for debug
		foreach (var anchor in graph.Keys.Where(a => a.Owner is Door))
		{
			GD.Print($"🚪 Door anchor at {anchor.Position} has {graph[anchor].Count} connections.");
			foreach (var connected in graph[anchor])
				GD.Print($"     ↳ Connected to {connected.Position} ({connected.Owner?.GetType().Name})");
		}

		for (int i = 0; i < doorAnchors.Count; i++)
		{
			Anchor doorAnchor = doorAnchors[i];

			// 1. Check path to other doors
			for (int j = i + 1; j < doorAnchors.Count; j++)
			{
				List<Anchor> path = FindPath(doorAnchor, doorAnchors[j]);
				if (path?.Count > 0)
				{
					GD.Print($"✅ Path found between Door {i} and Door {j}");
					DrawPath(path, room);
				}
				else GD.PrintErr($"❌ No path found between Door {i} and Door {j}");
			}

			// 2. Check path to any collectible
			bool foundKey = collectibleAnchors.Any(keyAnchor =>
			{
				var path = FindPath(doorAnchor, keyAnchor);
				if (path?.Count > 0)
				{
					GD.Print($"✅ Path from Door {i} to collectible at {keyAnchor.Position}");
					DrawPath(path, room);
					return true;
				}
				return false;
			});

			if (!foundKey)
				GD.PrintErr($"❌ No path from Door {i} to ANY collectible!");
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

			//if (a.Owner == b.Owner)
				//continue; // only allow internal connections from InternalPaths
				//
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
