using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class Zone
{
	// top-left corner coordinates
	public int X { get; }

	public int Y { get; }

	// dimensions of the zone
	public int Width { get; }

	public int Height { get; }

	// flag used to track reachability during connectivity analysis
	public bool isReachable { get; set; }

	// constructor sets position and size
	public Zone(int x, int y, int width, int height)
	{
		X = x;
		Y = y;
		Width = width;
		Height = height;
	}
}

public class BSPNode
{
	// node rectangle position and size
	public int X { get; private set; }
	public int Y { get; private set; }
	public int Width { get; private set; }
	public int Height { get; private set; }

	// child nodes after split
	public BSPNode left;
	public BSPNode right;

	// constructor sets area boundaries
	public BSPNode(int x, int y, int width, int height)
	{
		X = x;
		Y = y;
		Width = width;
		Height = height;
	}

	// recursive method splits node into smaller parts
	public void Split(int depth)
	{
		if (depth <= 0)
			return;

		Random random = new Random();

		// decide split direction based on dimensions
		bool splitHorizontally = Width > Height;

		if (splitHorizontally)
		{
			// choose random horizontal split point between 25% and 75%
			float ratio = (float)random.NextDouble() * 0.5f + 0.25f;
			int splitX = (int)(Width * ratio);

			left = new BSPNode(X, Y, splitX, Height);
			right = new BSPNode(X + splitX, Y, Width - splitX, Height);
		}
		else
		{
			// choose random vertical split point between 15% and 75%
			float ratio = (float)random.NextDouble() * 0.6f + 0.15f;
			int splitY = (int)(Height * ratio);

			left = new BSPNode(X, Y, Width, splitY);
			right = new BSPNode(X, Y + splitY, Width, Height - splitY);
		}

		// recursively split child nodes
		left.Split(depth - 1);
		right.Split(depth - 1);
	}

	// returns all leaf nodes of the current subtree
	public List<BSPNode> GetLeaves()
	{
		List<BSPNode> leaves = new List<BSPNode>();
		
		if (left == null && right == null)
		{
			leaves.Add(this);
		}
		else
		{
			if (left != null)
				leaves.AddRange(left.GetLeaves());
			if (right != null)
				leaves.AddRange(right.GetLeaves());
		}

		return leaves;
	}
}

public class ZoneHandler
{
	private Room room;
	private List<Zone> zones = new List<Zone>();
	private int minZoneWidth = 1;
	private int minZoneHeight = 3;
	private int splitDepth = 20; 
	
	public ZoneHandler(Room room)
	{
		this.room = room;
	}

	public List<Zone> GetZones() => zones;

	public void GenerateZones()
	{
		zones.Clear();

		// calculate tree depth based on room size and minimum zone dimensions
		int splitDepth = CalculateSplitDepth(room.Width, room.Height, minZoneWidth, minZoneHeight);

		// initialize root node and split
		BSPNode root = new BSPNode(0, 0, room.Width, room.Height);
		root.Split(splitDepth);

		// collect leaf nodes from split tree
		List<BSPNode> leaves = root.GetLeaves();
		foreach (var leaf in leaves)
		{
			Zone zone = new Zone(leaf.X, leaf.Y, leaf.Width, leaf.Height);
			zones.Add(zone);
		}
		
		// retry generation if unreachable zones form horizontal strip
		if(HasUnreachableZoneCluster()) {
			zones.Clear();
			GenerateZones();
		} else {
			foreach (Zone zone in zones) {
				zone.isReachable = false;
				GenerateFloorForZone(zone); // spawn floor primitive
			} 
			CheckHorizontallyReachableZones(); // mark zones reachable by floor adjacency
		}
	}
	
	public bool HasUnreachableZoneCluster()
	{
		// try to walk from left to right using horizontally connected zones
		List<Zone> startingZones = zones.Where(z => z.X == 0 && z.Y != 0).ToList();

		foreach (Zone startZone in startingZones)
		{
			int totalWidth = startZone.Width;
			int currentX = startZone.X + startZone.Width;
			int currentY = startZone.Y;

			while (totalWidth < room.Width)
			{
				Zone nextZone = zones.FirstOrDefault(z => z.X == currentX &&
					(z.Y == currentY || z.Y == currentY + 1 || z.Y == currentY - 1));

				if (nextZone == null)
					break;

				totalWidth += nextZone.Width;
				currentX += nextZone.Width;
				currentY = nextZone.Y;

				if (totalWidth == room.Width)
				{
					return true;
				}
			}
		}

		return false;
	}

	private int CalculateSplitDepth(int width, int height, int minZoneWidth, int minZoneHeight)
	{
		// estimate depth by averaging required splits along x and y
		int horizontalSplits = (int)Math.Floor(Math.Log2(width / (float)minZoneWidth));
		int verticalSplits = (int)Math.Floor(Math.Log2(height / (float)minZoneHeight));
		return Math.Max(1, (horizontalSplits + verticalSplits) / 2);
	}

	private void GenerateFloorForZone(Zone zone)
	{
		Floor floor = new Floor();
		floor.zone = zone;
		floor.GenerateInRoom(room);
	}
	
	public void CheckHorizontallyReachableZones()
	{
		foreach (Zone zone in zones)
		{
			// check if floor exists to left and right (or wall), within ±1 tile in y
			if ((zones.Any(z => z.X + z.Width == zone.X && Math.Abs(z.Y + z.Height - zone.Y - zone.Height) <= 1)
			|| zone.X == 0)
			&& (zones.Any(z => z.X == zone.X + zone.Width && Math.Abs(z.Y + z.Height - zone.Y - zone.Height) <= 1)
			|| zone.X + zone.Width == room.Width)
			) { 
				zone.isReachable = true;
			} 
		}
	}
	
	public List<(Zone from, Zone to)> GetVerticallyAdjacentZones()
	{
		List<(Zone from, Zone to)> connections = new();

		foreach (Zone zone in zones)
		{
			foreach (Zone other in zones)
			{
				bool horizontalOverlap = 
					(zone.X < other.X + other.Width && zone.X + zone.Width > other.X);
				bool directlyAbove = zone.Y + zone.Height == other.Y;

				if (horizontalOverlap && directlyAbove)
				{
					connections.Add((zone, other));
				}
			}
		}

		return connections;
	}
	
	public void placePlatforms() {
		// todo: implement platform placement logic
	}
	
	public void ConnectZonesVertically(Room room)
	{ 		
		placePlatforms(); // placeholder for platform placement

		foreach (Zone upper in zones)
		{
			foreach (Zone lower in zones)
			{
				if (lower == upper || upper.isReachable) continue;

				if (upper.Y + upper.Height <= lower.Y + lower.Height)
				{
					int left = Math.Max(lower.X, upper.X);
					int right = Math.Min(lower.X + lower.Width, upper.X + upper.Width);
					int overlapWidth = right - left;

					// ensure proper zone alignment for connection
					if (upper.X + upper.Width == lower.X + lower.Width || upper.X == lower.X
					|| upper.X == lower.X + lower.Width || upper.X + upper.Width == lower.X)
					{
						int verticalGap = lower.Y + lower.Height - (upper.Y + upper.Height);

						if (verticalGap > 2){
							if (upper.X + upper.Width == lower.X) {
								if (canPlaceSpring(upper, verticalGap, true)) {
									upper.isReachable = PlaceSpring(upper, verticalGap, true);
								} 
								if (canPlaceSlope(upper, verticalGap, true) && !upper.isReachable) {
									upper.isReachable = PlaceSlope(upper, verticalGap, true); 
								} 
								if (!upper.isReachable) {
									upper.isReachable = PlaceLadder(upper, verticalGap, true);
								}
							} else if (lower.X + lower.Width == upper.X) {
								if (canPlaceSpring(upper, verticalGap, false) ) {
									upper.isReachable = PlaceSpring(upper, verticalGap, false);
								} 
								if (canPlaceSlope(upper, verticalGap, false) && !upper.isReachable) {
									upper.isReachable = PlaceSlope(upper, verticalGap, false); 
								} 
								if (!upper.isReachable) {
									upper.isReachable = PlaceLadder(upper, verticalGap, false);
								}
							}
						}
					}
				}
			}
		}
		
		foreach (Zone zone in zones)
		{ 
			if (!zone.isReachable) {
				// log unreachable zone
			}
		}
	}
	
	bool canPlaceSpring(Zone upper, int verticalGap, bool right) {
		Vector2 position; 
		if (right) {
			position = new Vector2(upper.X + upper.Width, upper.Y + upper.Height + verticalGap - 2);
		} else {
			position = new Vector2(upper.X - 1, upper.Y + upper.Height + verticalGap - 2);
		}
		
		int springJumpHeight = Mathf.FloorToInt((room.Player.springJumpSpeed * room.Player.springJumpSpeed) / (2f * room.Player.gravity * 70f));
		
		for (int y = 1; y < springJumpHeight + 1; y++) {
			if (room.HasAtomAt((position - new Vector2(0, y)) * new Vector2(70, 70))) { // if obstruction in airspace above spring
				return false;
			}
		}
			
		return verticalGap <= springJumpHeight && room.HasAtomBelow(position * new Vector2(70, 70), typeof(FloorTile));
	}
	
	bool canPlaceSlope(Zone upper, int length, bool right) {
		
		Vector2 position; 
		if (right) {
			position = new Vector2(upper.X + upper.Width + length - 1, upper.Y + upper.Height + length - 2);
		} else {
			position = new Vector2(upper.X - length, upper.Y + upper.Height + length - 2);
		}
		
		int offset;
		if (right) { offset = -1; } else { offset = 1; }
				
		for (int x = 0; x < length + 1; x++) {
			if (!room.HasAtomBelow((position + new Vector2(x * offset, 0)) * new Vector2(70, 70), typeof(FloorTile)) 
			|| room.HasAtomAt((position + new Vector2(x * offset, -x-1)) * new Vector2(70, 70))
			|| room.HasAtomAt((position + new Vector2(x * offset, -x-2)) * new Vector2(70, 70))) { // if tile above slope or no floor tile under slope base
				return false; 
			} 
		}
		return true;
	}
	
	bool canPlacePlatformPath(Zone upper, int verticalGap, bool right) {
		return verticalGap >= 3;
	}
	
	bool PlaceLadder(Zone upper, int verticalGap, bool right)
	{
		//GD.Print("🪜 Use a ladder.");
		
		Vector2 position; 
		if (right) {
			position = new Vector2(upper.X + upper.Width, upper.Y + upper.Height - 2);
		} else {
			position = new Vector2(upper.X - 1, upper.Y + upper.Height - 2);
		}
		
		Ladder ladder = new Ladder();
		ladder.position = position;
		ladder.length = verticalGap + 1;
		return ladder.GenerateInRoom(room);
	}
	
	bool PlaceSlope(Zone upper, int verticalGap, bool right)
	{
		//GD.Print("🧗 Use a slope.");
		
		Vector2 position; 
		if (right) {
			position = new Vector2(upper.X + upper.Width + verticalGap - 1, upper.Y + upper.Height + verticalGap - 2);
		} else {
			position = new Vector2(upper.X - verticalGap, upper.Y + upper.Height + verticalGap - 2);
		}
		
		Vector2 worldPosition = position * new Vector2(70, 70);
		bool placedSlope = false;
				
		if (right) {
			LeftSlope slope = new LeftSlope();
			slope.position = worldPosition;
			slope.length = verticalGap;
			slope.zone = upper;
			placedSlope = slope.GenerateInRoom(room);
		} else {
			RightSlope slope = new RightSlope();
			slope.position = worldPosition;
			slope.length = verticalGap;
			slope.zone = upper;
			placedSlope = slope.GenerateInRoom(room);
		}
		 return placedSlope;
	}
	
	bool PlaceSpring(Zone upper, int verticalGap, bool right) {
		Vector2 position; 
		if (right) {
			position = new Vector2(upper.X + upper.Width + 1, upper.Y + upper.Height + verticalGap - 2);
		} else {
			position = new Vector2(upper.X - 2, upper.Y + upper.Height + verticalGap - 2);
		}
		
		//GD.Print("🍄 Use a spring.");
		Mushroom spring = new Mushroom();
		spring.position = position * new Vector2(70, 70);
		return spring.GenerateInRoom(room);
	}
	
	void PlacePlatformPath(Zone upper, int verticalGap, bool right) {
		Vector2 position; 
		if (right) {
			position = new Vector2(upper.X + upper.Width + 2, upper.Y + upper.Height);
		} else {
			position = new Vector2(upper.X - 2, upper.Y + upper.Height);
		}
		
		//GD.Print("🧗 Use a platform.");
		Platform platform = new Platform();
		platform.position = position * new Vector2(70, 70);
		platform.GenerateInRoom(room);
	}


	public void DrawZoneBorders(Room room)
	{
		float tileSize = 70f;

		foreach (Zone zone in zones)
		{
			Line2D border = new Line2D();
			border.DefaultColor = new Color(1, 0, 1); // Red
			border.Width = 2;

			Vector2 topLeft = new Vector2(zone.X, zone.Y) * tileSize + new Vector2(5,35);
			Vector2 topRight = new Vector2(zone.X + zone.Width, zone.Y) * tileSize + new Vector2(5, 35);
			Vector2 bottomRight = new Vector2(zone.X + zone.Width, zone.Y + zone.Height) * tileSize + new Vector2(5, 35);
			Vector2 bottomLeft = new Vector2(zone.X, zone.Y + zone.Height) * tileSize + new Vector2(5, 35);

			border.AddPoint(topLeft);
			border.AddPoint(topRight);
			border.AddPoint(bottomRight);
			border.AddPoint(bottomLeft);
			border.AddPoint(topLeft); // Close the rectangle

			room.AddChild(border); // Attach border to room
		}
	}
}
