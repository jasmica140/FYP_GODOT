using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

// Holds various metrics used to calculate the final interestingness score
public class InterestingnessResult
{
	public float Score { get; set; }  // final weighted interestingness score
	public int AnchorsVisited { get; set; }  // how many unique anchors were visited during path traversal
	public int TotalAnchors { get; set; }  // total available anchors in the graph
	public int Goals { get; set; }  // how many paths were successfully completed
	public int MaxGoals { get; set; }  // max number of goals (usually total doors - 1)
	public float AverageDifficulty { get; set; }  // average difficulty across paths
	public int MaxDifficulty { get; set; }  // maximum expected difficulty
	public int VerticalModifiersUsed { get; set; }  // unique vertical movement primitives used
	public int MaxVerticalModifiers { get; set; }  // total available vertical movement primitive types
	public int PlayerAbilitiesUsed { get; set; }  // unique player ability-related primitives used
	public int MaxPlayerAbilities { get; set; }  // total types of player ability primitives considered
}

public partial class PathBuilder : Node2D
{
	private Room room;
	private Dictionary<Anchor, List<Anchor>> graph = new(); // stores reachable anchors
	private Random rng = new Random();

	public PathBuilder(Room room)
	{
		this.room = room;
	}

	// Generates key placements at "interesting" locations: pits, water, furthest reachable floor tiles
	public List<Primitive> GenerateKeysAtIdealPositions(Door targetDoor)
	{
		List<Primitive> idealKeys = new();
		List<Vector2> candidatePositions = new();
		int tileWidth = 70;

		// add the bottom-center of each pit
		foreach (Pit pit in room.Primitives.Where(p => p is Pit).Cast<Pit>().ToList()) {
			float x = pit.Position.X + ((pit.Width - 1) * tileWidth / 2) + 1;
			float y = pit.Position.Y + ((pit.Depth - 1) * tileWidth);
			Vector2 pos = new Vector2(x, y);
			if (!room.HasAtomAt(pos)) {
				candidatePositions.Add(pos);
			}
		}

		// add the bottom-center of each water body
		foreach (Water water in room.Primitives.Where(p => p is Water).Cast<Water>().ToList()) {
			float x = water.Position.X + ((water.Width - 1) * tileWidth / 2) + 1;
			float y = water.Position.Y + ((water.Depth - 1) * tileWidth);
			Vector2 pos = new Vector2(x, y);
			if (!room.HasAtomAt(pos)) {
				candidatePositions.Add(pos);
			}
		}

		// add the 4 furthest valid floor tiles from the target door
		var floorPositions = room.GetPositionsAboveFloorTiles()
			.Where(pos => !room.HasAtomAt(pos))
			.OrderByDescending(pos => (pos - targetDoor.Position).LengthSquared())
			.Take(4)
			.ToList();

		candidatePositions.AddRange(floorPositions);

		// convert those positions into DoorKey primitives
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

	// Builds the anchor graph using internal paths and anchor orbit overlaps
	public void BuildGraph(List<Primitive> primitives, List<Primitive> idealKeys)
	{
		graph.Clear();
		List<Anchor> allAnchors = new();
		List<Primitive> tempPrimitives = primitives.Concat(idealKeys).ToList();

		// initialize empty adjacency list
		foreach (var primitive in tempPrimitives)
		{
			foreach (var anchor in primitive.Anchors)
			{
				if (!graph.ContainsKey(anchor))
					graph[anchor] = new List<Anchor>();
			}
			allAnchors.AddRange(primitive.Anchors);
		}

		// add internal primitive connections
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

		// connect anchors with overlapping orbits (only between different primitives)
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
	}

	// Breadth-first search between two anchors
	public List<Anchor> FindPath(Anchor start, Anchor goal)
	{
		Queue<List<Anchor>> queue = new();
		HashSet<Anchor> visited = new();
		
		queue.Enqueue(new List<Anchor> { start });
		visited.Add(start);

		while (queue.Count > 0)
		{
			List<Anchor> path = queue.Dequeue();
			Anchor current = path.Last();

			if (current == goal)
				return path;

			if (!graph.ContainsKey(current)) continue;

			foreach (var neighbor in graph[current])
			{
				if (!visited.Contains(neighbor))
				{
					visited.Add(neighbor);
					var newPath = new List<Anchor>(path) { neighbor };
					queue.Enqueue(newPath);
				}
			}
		}
		return null;
	}

	// Sums the difficulty across a path, counting each primitive once
	private int EvaluatePathDifficulty(List<Anchor> path)
	{
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

	// Chooses best key-based path to a door by maximizing difficulty
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

	// Aggregates scores across all paths to compute final interestingness
	public InterestingnessResult CalculateInterestingness(List<List<Anchor>> paths)
	{
		int anchorsVisited = 0;
		int goals = paths.Count;
		int totalDifficulty = 0;
		int verticalMovementTypes = 0;
		int playerAbilities = 0;

		// Weights and max values used to scale the metrics
		float wa = 0.05f, wg = 0.2f, wd = 0.3f, wv = 0.2f, wp = 0.25f;
		int amax = graph.Count(), gmax = 3, dmax = 120, vmax = 4, pmax = 3;

		List<Anchor> distinctAnchorsVisited = new();
		List<Type> remainingVerticalMovementTypes = new() { typeof(Ladder), typeof(Mushroom), typeof(LeftSlope), typeof(RightSlope) };
		List<Type> remainingAbilityTypes = new() { typeof(Water), typeof(Pit), typeof(FullBlade) };

		foreach (List<Anchor> path in paths) {
			totalDifficulty += EvaluatePathDifficulty(path);
			foreach (Anchor anchor in path) {
				if (!distinctAnchorsVisited.Contains(anchor)) {
					distinctAnchorsVisited.Add(anchor);
				}
				remainingVerticalMovementTypes.Remove(anchor.Owner.GetType());
				remainingAbilityTypes.Remove(anchor.Owner.GetType());
			}
		}

		anchorsVisited = distinctAnchorsVisited.Count;
		verticalMovementTypes = vmax - remainingVerticalMovementTypes.Count;
		playerAbilities = pmax - remainingAbilityTypes.Count;

		float sa = (float)anchorsVisited / amax;
		float sg = (float)goals / gmax;
		float avgDifficulty = (float)totalDifficulty / paths.Count;
		float sd = avgDifficulty / dmax;
		float sv = (float)verticalMovementTypes / vmax;
		float sp = (float)playerAbilities / pmax;

		float interestingness = sa * wa + sg * wg + sd * wd + sv * wv + sp * wp;

		GD.Print($"📊 Interestingness Breakdown:");
		GD.Print($"🔹 Anchors Visited: {anchorsVisited}/{amax} → {sa:F2} * {wa} = {sa * wa:F2}");
		GD.Print($"🔹 Goals: {goals}/{gmax} → {sg:F2} * {wg} = {sg * wg:F2}");
		GD.Print($"🔹 Avg Difficulty: {avgDifficulty:F0}/{dmax} → {sd:F2} * {wd} = {sd * wd:F2}");
		GD.Print($"🔹 Vertical Modifiers Used: {verticalMovementTypes}/{vmax} → {sv:F2} * {wv} = {sv * wv:F2}");
		GD.Print($"🔹 Player Abilities Used: {playerAbilities}/{pmax} → {sp:F2} * {wp} = {sp * wp:F2}");
		GD.Print($"✨ Final Interestingness Score: {interestingness:F2}");

		return new InterestingnessResult {
			Score = interestingness,
			AnchorsVisited = anchorsVisited,
			TotalAnchors = amax,
			Goals = goals,
			MaxGoals = gmax,
			AverageDifficulty = avgDifficulty,
			MaxDifficulty = dmax,
			VerticalModifiersUsed = verticalMovementTypes,
			MaxVerticalModifiers = vmax,
			PlayerAbilitiesUsed = playerAbilities,
			MaxPlayerAbilities = pmax
		};
	}

	// Calls pathfinding from the leftmost door to all other doors
	public InterestingnessResult GeneratePathsFromStartDoor()
	{
		List<List<Anchor>> bestPaths = new();

		List<Door> allDoors = room.Primitives.OfType<Door>().ToList();
		if (allDoors.Count < 2) return null;

		Door startDoor = allDoors.OrderBy(d => d.Position.X).First();
		var startAnchor = startDoor.Anchors.FirstOrDefault(a => a.Type == "center");
		if (startAnchor == null) return null;

		startDoor.OpenDoor(room);
		startDoor.isStartDoor = true;

		foreach (Door targetDoor in allDoors)
		{
			if (targetDoor == startDoor) continue;
			var targetAnchor = targetDoor.Anchors.FirstOrDefault(a => a.Type == "center");
			if (targetAnchor == null) continue;

			List<Anchor> path = FindBestPath(startAnchor, targetAnchor, GenerateKeysAtIdealPositions(targetDoor));
			if (path == null || path.Count == 0) {
				GD.PrintErr($"❌ No path to {targetDoor.Colour} door");
				continue;
			}

			Color doorColour = GetColourFromDoor(targetDoor.Colour);
			DrawPath(path, doorColour, room);
			bestPaths.Add(path);
		}

		return CalculateInterestingness(bestPaths);
	}

	public Color GetColourFromDoor(DoorColour colour)
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

	// Visually renders the path on screen for debug or screenshots
	public void DrawPath(List<Anchor> path, Color colour, Room room)
	{
		for (int i = 0; i < path.Count - 1; i++)
		{
			var a = path[i].Position;
			var b = path[i + 1].Position;
			room.ColouredDebugPathLines.Add((a, b, colour));
		}
	}

	public List<(Vector2 start, Vector2 end, Color colour)> ColouredDebugPathLines = new();

	public override void _Draw()
	{
		foreach (var (start, end, colour) in ColouredDebugPathLines)
		{
			DrawLine(start + new Vector2(40, 60), end + new Vector2(40, 60), colour, 6f);
		}
	}
}
