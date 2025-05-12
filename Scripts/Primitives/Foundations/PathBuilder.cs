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
	}
	
	// Step 1: Generate ideal anchors based on pits, water, and furthest floor tiles
	public List<Primitive> GenerateKeysAtIdealPositions(Door targetDoor)
	{
		List<Primitive> idealKeys = new();
		List<Vector2> candidatePositions = new();
	
		int tileWidth = 70;

		// 1. Add bottoms of pits
		foreach (Pit pit in room.Primitives.Where(p => p is Pit).Cast<Pit>().ToList()) {
			float x = pit.Position.X + ((pit.Width - 1) * tileWidth / 2) + 1; 
			float y = pit.Position.Y + ((pit.Depth - 1) * tileWidth);
			Vector2 pos = new Vector2(x, y);
			if (!room.HasAtomAt(pos)) {
				candidatePositions.Add(pos);
			}
		}
	
		// 2. Add bottoms of water
		foreach (Water water in room.Primitives.Where(p => p is Water).Cast<Water>().ToList()) {
			float x = water.Position.X + ((water.Width - 1) * tileWidth / 2) + 1; 
			float y = water.Position.Y + ((water.Depth - 1) * tileWidth);
			Vector2 pos = new Vector2(x, y);
			if (!room.HasAtomAt(pos)) {
				candidatePositions.Add(pos);
			}
		}
	
		// 3. Get 4 furthest floor tile positions from the target door
		var floorPositions = room.GetPositionsAboveFloorTiles();
		floorPositions = floorPositions
			.Where(pos => !room.HasAtomAt(pos)) // ensure tile is not already occupied 
			.OrderByDescending(pos => (pos - targetDoor.Position).LengthSquared())
			.Take(4)
			.ToList();
	
		candidatePositions.AddRange(floorPositions);
	
		// 4. Convert positions to temporary key primtives
		foreach (var pos in candidatePositions) {
			DoorKey doorKey = new DoorKey {
				Position = pos,
				Colour = targetDoor.Colour
			};
			doorKey.GenerateAnchors(room);
			idealKeys.Add(doorKey);
		}
	
		return idealKeys;
	}
	
	public void BuildGraph(List<Primitive> primitives, List<Primitive> idealKeys)
	{
		graph.Clear();
		List<Anchor> allAnchors = new List<Anchor>();
		
		List<Primitive> tempPrimitives = primitives.Concat(idealKeys).ToList();
		
		// Step 0: Gather all anchors and add them to the graph with empty lists
		foreach (var primitive in tempPrimitives)
		{
			foreach (var anchor in primitive.Anchors)
			{
				if (!graph.ContainsKey(anchor))
					graph[anchor] = new List<Anchor>();
			}

			allAnchors.AddRange(primitive.Anchors);
		}

		// Step 1: Add internal connections
		foreach (var primitive in tempPrimitives)
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
		
		GD.Print($"Graph built with {graph.Count} nodes.");
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

			if (!graph.ContainsKey(current)) { continue; }

			foreach (var neighbor in graph[current])
			{
				if (!visited.Contains(neighbor))
				{
					//GD.Print($"➡️ Exploring neighbor: {neighbor.Position}");
					visited.Add(neighbor);
					var newPath = new List<Anchor>(path) { neighbor };
					queue.Enqueue(newPath);
				}
			}
		}

		GD.PrintErr($"❌ No path found from {start.Position} to {goal.Position}");
		return null;
	}
	
	private int EvaluatePathDifficulty(List<Anchor> path) {
		
		int Difficulty = 0;
		Primitive currentOwner = null;
		foreach (Anchor anchor in path) {
			if (anchor.Owner != currentOwner) {
				Difficulty += anchor.Owner.Difficulty;
				currentOwner = anchor.Owner;
			}
		}
		return Difficulty;
	}
	
	public List<Anchor> FindBestPath(Anchor start, Anchor goal, List<Primitive> idealKeys)
	{
		BuildGraph(room.Primitives, idealKeys);
		
		List<Anchor> bestPath = null;
		int bestScore = 0;
		Primitive bestKey = null;
		
		foreach (Primitive idealKey in idealKeys) {
			List<Anchor> pathToKey = FindPath(start, idealKey.Anchors.First());
			List<Anchor> pathFromKey = FindPath(idealKey.Anchors.First(), goal);
			if (pathToKey != null && pathFromKey != null) {
				int score = EvaluatePathDifficulty(pathToKey) + EvaluatePathDifficulty(pathFromKey);
				if (score > bestScore){
					bestPath = pathToKey.Concat(pathFromKey).ToList();
					bestScore = score;
					bestKey = idealKey;
				}
			}
		}
		
		if (bestKey != null) {
			bestKey.GenerateInRoom(room);
		}
		
		return bestPath;
	}
	
	public void GenerateKeysFromStartDoor()
	{
		List<Door> allDoors = room.Primitives.OfType<Door>().ToList();
		if (allDoors.Count < 2) {
			GD.Print("🚪 Not enough doors to generate paths.");
			return;
		}

		// Step 1: Pick a start door (e.g. leftmost one)
		Door startDoor = allDoors.OrderBy(d => d.Position.X).First();
		var startAnchor = startDoor.Anchors.FirstOrDefault(a => a.Type == "center");
		if (startAnchor == null) {
			GD.PrintErr("❌ Start door has no center anchor.");
			return;
		}
		
		startDoor.OpenDoor(room);
		startDoor.isStartDoor = true;
		
		GD.Print($"🚪 Chosen start door: {startDoor.Colour} at {startDoor.Position}");

		// Step 2: For each other door, find a path
		foreach (Door targetDoor in allDoors)
		{			
			if (targetDoor == startDoor) continue;

			var targetAnchor = targetDoor.Anchors.FirstOrDefault(a => a.Type == "center");
			if (targetAnchor == null) continue;
			
			List<Anchor> path = FindBestPath(startAnchor, targetAnchor, GenerateKeysAtIdealPositions(targetDoor));
			if (path == null || path.Count == 0)
			{
				GD.PrintErr($"❌ No path to {targetDoor.Colour} door");
				continue;
			}

			// 🎨 Draw the path in the door's color
			Color doorColor = GetColorFromDoor(targetDoor.Colour);
			DrawPath(path, doorColor, room);

			int difficulty = EvaluatePathDifficulty(path);
			GD.Print($"✅ Path to {targetDoor.Colour}: {path.Count} steps, difficulty {difficulty}");
		}
	}
	
	public Color GetColorFromDoor(DoorColour colour)
	{
		return colour switch
		{
			DoorColour.Red => Colors.Red,
			DoorColour.Blue => Colors.Blue,
			DoorColour.Green => Colors.Green,
			DoorColour.Yellow => Colors.Yellow,
			_ => Colors.White
		};
	}

	public void DrawPath(List<Anchor> path, Color color, Room room)
	{
		for (int i = 0; i < path.Count - 1; i++)
		{
			var a = path[i].Position;
			var b = path[i + 1].Position;
			room.ColoredDebugPathLines.Add((a, b, color));
		}
	}
}
