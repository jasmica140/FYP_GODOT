using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class Zone
{
	public int X { get; }
	public int Y { get; }
	public int Width { get; }
	public int Height { get; }
	public bool isReachable { get; set; }

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
	public int X { get; private set; }
	public int Y { get; private set; }
	public int Width { get; private set; }
	public int Height { get; private set; }

	public BSPNode left;
	public BSPNode right;

	public BSPNode(int x, int y, int width, int height)
	{
		X = x;
		Y = y;
		Width = width;
		Height = height;
	}
	
	public void Split(int depth)
	{
		if (depth <= 0)
			return;

		Random random = new Random();
		bool splitHorizontally = Width > Height;

		if (splitHorizontally)
		{
			float ratio = (float)random.NextDouble() * 0.5f + 0.25f; // Between 25% and 75%
			int splitX = (int)(Width * ratio);

			left = new BSPNode(X, Y, splitX, Height);
			right = new BSPNode(X + splitX, Y, Width - splitX, Height);
		}
		else
		{
			float ratio = (float)random.NextDouble() * 0.6f + 0.15f; // between 0.15 and 0.75
			int splitY = (int)(Height * ratio);

			left = new BSPNode(X, Y, Width, splitY);
			right = new BSPNode(X, Y + splitY, Width, Height - splitY);
		}

		left.Split(depth - 1);
		right.Split(depth - 1);
	}
	
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

		int splitDepth = CalculateSplitDepth(room.Width, room.Height, minZoneWidth, minZoneHeight);
		BSPNode root = new BSPNode(0, 0, room.Width, room.Height);
		root.Split(splitDepth);

		List<BSPNode> leaves = root.GetLeaves();
		foreach (var leaf in leaves)
		{
			Zone zone = new Zone(leaf.X, leaf.Y, leaf.Width, leaf.Height);
			zones.Add(zone);
		}
		
		if(HasUnreachableZoneCluster()) {
			zones.Clear();
			GenerateZones();
		} else {
			foreach (Zone zone in zones) {
				GD.Print($"🟦 Generated Zone: X={zone.X}, Y={zone.Y}, W={zone.Width}, H={zone.Height}");
				zone.isReachable = false;
				GenerateFloorForZone(zone);
			} 
			CheckHorizontallyReachableZones();
		}
	}
	
	public bool HasUnreachableZoneCluster()
	{
		List<Zone> startingZones = zones.Where(z => z.X == 0 && z.Y != 0).ToList(); // Skip zones at Y == 0

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
					GD.Print("⚠️ Unreachable zone cluster detected. Suggest regenerating zones.");
					return true;
				}
			}
		}

		return false;
	}

	private int CalculateSplitDepth(int width, int height, int minZoneWidth, int minZoneHeight)
	{
		int horizontalSplits = (int)Math.Floor(Math.Log2(width / (float)minZoneWidth));
		int verticalSplits = (int)Math.Floor(Math.Log2(height / (float)minZoneHeight));
		return Math.Max(1, (horizontalSplits + verticalSplits) / 2); // Ensure at least one split
	}

	private void GenerateFloorForZone(Zone zone)
	{
		Floor floor = new Floor();
		floor.zone = zone;
		floor.GenerateInRoom(room);
		//floor.GenerateAnchors();
	}
	
	public void CheckHorizontallyReachableZones()
	{
		foreach (Zone zone in zones)
		{
			// if zone floor is between two zone floors ±1y or between wall and floor
			if ((zones.Any(z => z.X + z.Width == zone.X && Math.Abs(z.Y + z.Height - zone.Y - zone.Height) <= 1) // there is a zone floor to the left ±1y
			|| zone.X == 0) // or a wall to the left 
			&& (zones.Any(z => z.X == zone.X + zone.Width && Math.Abs(z.Y + z.Height - zone.Y - zone.Height) <= 1) // and floor to the right
			|| zone.X + zone.Width == room.Width) // or wall to the right
			) { 
				GD.Print($"zone at ({zone.X},{zone.Y}) is reachable.");
				zone.isReachable = true; // zone must be reachable ✅
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
				// Check for horizontal overlap and if the other zone is directly above this one
				bool horizontalOverlap = 
					(zone.X < other.X + other.Width && zone.X + zone.Width > other.X);

				bool directlyAbove = zone.Y + zone.Height == other.Y;

				if (horizontalOverlap && directlyAbove)
				{
					connections.Add((zone, other));
					GD.Print($"🔗 Vertical connection found between zone at ({zone.X}, {zone.Y}) and ({other.X}, {other.Y})");
				}
			}
		}

		return connections;
	}
	
	public void placePlatforms() {
		// add logic for situations when to add platforms 
	}
	
	public void ConnectZonesVertically(Room room)
	{ 		
		placePlatforms();
		
		foreach (Zone upper in zones)
		{
			foreach (Zone lower in zones)
			{
				// Skip if it's the same zone
				if (lower == upper || upper.isReachable) continue;

				// Only check zones where the second one is higher up
				if (upper.Y + upper.Height <= lower.Y + lower.Height)
				{
					
					// Check if there's any horizontal overlap
					int left = Math.Max(lower.X, upper.X);
					int right = Math.Min(lower.X + lower.Width, upper.X + upper.Width);
					int overlapWidth = right - left;

					if (upper.X + upper.Width == lower.X + lower.Width || upper.X == lower.X
					|| upper.X == lower.X + lower.Width || upper.X + upper.Width == lower.X) // Ensure there’s at least 1 tile of overlap
					{
						int verticalGap = lower.Y + lower.Height - (upper.Y + upper.Height);
						if (verticalGap > 2){
							GD.Print($"🔍 Vertical connection possible from Zone at ({lower.X},{lower.Y}) to ({upper.X},{upper.Y})");
							GD.Print($"➡️ Overlap Width: {overlapWidth}, Vertical Gap: {verticalGap}");

							// Decision logic — for now, just print which primitive would make sense
							if (verticalGap <= 0) {
								GD.Print("📦 Use a platform.");
							}							
							
							if (upper.X + upper.Width == lower.X) { // if lower is on right - place on right
								//if (canPlacePlatformPath(upper, verticalGap, true)) {
									//PlacePlatformPath(upper, verticalGap, true);
									//GD.Print($"zone at ({upper.X},{upper.Y}) is reachable.");
									//upper.isReachable = true;
								//} else 
								
								if (canPlaceSpring(upper, verticalGap, true)) { // on left or right and no obstacles withing jumping area
									upper.isReachable = PlaceSpring(upper, verticalGap, true);
									GD.Print($"zone at ({upper.X},{upper.Y}) is reachable.");
								} 
								if (canPlaceSlope(upper, verticalGap, true) && !upper.isReachable) { // enough floor space below
									upper.isReachable = PlaceSlope(upper, verticalGap, true); 
									GD.Print($"zone at ({upper.X},{upper.Y}) is reachable.");
								} 
								if (!upper.isReachable) { // ladder on the right
									upper.isReachable = PlaceLadder(upper, verticalGap, true);
									GD.Print($"zone at ({upper.X},{upper.Y}) is reachable.");
								}
								
							} else if (lower.X + lower.Width == upper.X) { // if lower is on left - place on left
								//if (canPlacePlatformPath(upper, verticalGap, false)) {
									//PlacePlatformPath(upper, verticalGap, false);
									//GD.Print($"zone at ({upper.X},{upper.Y}) is reachable.");
									//upper.isReachable = true;
								//} else 
								
								if (canPlaceSpring(upper, verticalGap, false) ) { // on left or right and no obstacles withing jumping area
									upper.isReachable = PlaceSpring(upper, verticalGap, false);
									GD.Print($"zone at ({upper.X},{upper.Y}) is reachable.");
								} 
								if (canPlaceSlope(upper, verticalGap, false) && !upper.isReachable) { // enough floor space below
									upper.isReachable = PlaceSlope(upper, verticalGap, false); 
									GD.Print($"zone at ({upper.X},{upper.Y}) is reachable.");
								} 
								if (!upper.isReachable) { // ladder on the left
									upper.isReachable = PlaceLadder(upper, verticalGap, false);
									GD.Print($"zone at ({upper.X},{upper.Y}) is reachable.");
								}
							}
						} else {
								GD.Print("❌ No vertical connection possible.");
						}
					}
				}
			}
		}
		
		foreach (Zone zone in zones)
		{ 
			if (!zone.isReachable) {
				GD.Print($"zone at ({zone.X},{zone.Y}) is not reachable!");
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
		GD.Print("🪜 Use a ladder.");
		
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
		GD.Print("🧗 Use a slope.");
		
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
			//slope.GenerateAnchors();
		} else {
			RightSlope slope = new RightSlope();
			slope.position = worldPosition;
			slope.length = verticalGap;
			slope.zone = upper;
			placedSlope = slope.GenerateInRoom(room);
			//slope.GenerateAnchors();
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
		
		GD.Print("🍄 Use a spring.");
		Mushroom spring = new Mushroom();
		spring.position = position * new Vector2(70, 70);
		return spring.GenerateInRoom(room);
		//spring.GenerateAnchors();
	}
	
	void PlacePlatformPath(Zone upper, int verticalGap, bool right) {
		Vector2 position; 
		if (right) {
			position = new Vector2(upper.X + upper.Width + 2, upper.Y + upper.Height);
		} else {
			position = new Vector2(upper.X - 2, upper.Y + upper.Height);
		}
		
		GD.Print("🧗 Use a platform.");
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

//else if (upper.X == 0 && !zones.Any(z => z.X == upper.X + upper.Width && z.Y + z.Height == upper.Y + upper.Height))
							//{ 	// Left wall and no floor to the right - place on right
								//if (canPlaceSpring(upper, verticalGap, true)) { // on left or right and no obstacles withing jumping area
									//PlaceSpring(upper, verticalGap, true);
								//} else if (canPlaceSlope(upper, verticalGap, true)) { // enough floor space below
									//placedConnection = PlaceSlope(upper, verticalGap, true);  
								//} 
								//if (!placedConnection) {
									//PlaceLadder(upper, verticalGap, true);
								//}
							//}
							//else if (upper.X + upper.Width == room.Width && !zones.Any(z => z.X + z.Width == upper.X && z.Y + z.Height == upper.Y + upper.Height))
							//{ 	// Right wall and no floor to the left - place on left
								//if (canPlaceSpring(upper, verticalGap, false)) { // on left or right and no obstacles withing jumping area
									//PlaceSpring(upper, verticalGap, false);
								//} else if (canPlaceSlope(upper, verticalGap, false)) { // enough floor space below
									//placedConnection = PlaceSlope(upper, verticalGap, false); 
								//} 
								//if (!placedConnection) {
									//PlaceLadder(upper, verticalGap, false); 
								//}
							//}
							//else if (zones.Any(z => z.X == upper.X + upper.Width && z.Y + z.Height == upper.Y + upper.Height) && upper.X != 0)
							//{ 	// Floor to the right and no wall to the left - place on left
								//if (canPlaceSpring(upper, verticalGap, false)) { // on left or right and no obstacles withing jumping area
									//PlaceSpring(upper, verticalGap, false);
								//} else if (canPlaceSlope(upper, verticalGap, false)) { // enough floor space below
									//placedConnection = PlaceSlope(upper, verticalGap, false); 
								//} 
								//if (!placedConnection) { // ladder on the left
									//PlaceLadder(upper, verticalGap, false);
								//}
							//}
							//else if (zones.Any(z => z.X + z.Width == upper.X && z.Y + z.Height == upper.Y + upper.Height) && upper.X + upper.Width != room.Width)
							//{ 	// Floor to the left and no wall to the right - place on right
								//if (canPlaceSpring(upper, verticalGap, true)) { // on left or right and no obstacles withing jumping area
									//PlaceSpring(upper, verticalGap, true);
								//} else if (canPlaceSlope(upper, verticalGap, true)) { // enough floor space below
									//placedConnection = PlaceSlope(upper, verticalGap, true); 
								//} 
								//if (!placedConnection) { // ladder on the right
									//PlaceLadder(upper, verticalGap, true);
								//}
							//}
							//else if ( !zones.Any(z => z.X + z.Width == upper.X && z.Y + z.Height == upper.Y + upper.Height) 
									//&& !zones.Any(z => z.X == upper.X + upper.Width && z.Y + z.Height == upper.Y + upper.Height) 
									//&& upper.X != 0 && upper.X + upper.Width != room.Width)
							//{ 	// No wall or floor adjacent 
								//if (upper.X + upper.Width == lower.X) { // if lower is on right - place on right
									//if (canPlaceSpring(upper, verticalGap, true)) { // on left or right and no obstacles withing jumping area
									//PlaceSpring(upper, verticalGap, true);
									//} else if (canPlaceSlope(upper, verticalGap, true)) { // enough floor space below
										//placedConnection = PlaceSlope(upper, verticalGap, true); 
									//} 
									//if (!placedConnection) { // ladder on the right
										//PlaceLadder(upper, verticalGap, true);
									//}
								//} else if (lower.X + lower.Width == upper.X) { // if lower is on left - place on left
									//if (canPlaceSpring(upper, verticalGap, false)) { // on left or right and no obstacles withing jumping area
									//PlaceSpring(upper, verticalGap, false);
									//} else if (canPlaceSlope(upper, verticalGap, false)) { // enough floor space below
										//placedConnection = PlaceSlope(upper, verticalGap, false); 
									//} 
									//if (!placedConnection) { // ladder on the left
										//PlaceLadder(upper, verticalGap, false);
									//}
								//}
							//}
