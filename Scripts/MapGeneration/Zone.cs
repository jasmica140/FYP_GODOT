using Godot;
using System;
using System.Collections.Generic;

public class Zone
{
	public int X { get; }
	public int Y { get; }
	public int Width { get; }
	public int Height { get; }

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
	private int minZoneWidth = 5;
	private int minZoneHeight = 5;
	private int splitDepth = 6; 
	
	public ZoneHandler(Room room)
	{
		this.room = room;
	}

	public List<Zone> GetZones() => zones;

	public void GenerateZones()
	{
		zones.Clear();

		int minZoneWidth = 3;
		int minZoneHeight = 3;

		int splitDepth = CalculateSplitDepth(room.Width, room.Height, minZoneWidth, minZoneHeight);
		BSPNode root = new BSPNode(0, 0, room.Width, room.Height);
		root.Split(splitDepth);

		List<BSPNode> leaves = root.GetLeaves();
		foreach (var leaf in leaves)
		{
			Zone zone = new Zone(leaf.X, leaf.Y, leaf.Width, leaf.Height);
			zones.Add(zone);
			GD.Print($"🟦 Generated Zone: X={zone.X}, Y={zone.Y}, W={zone.Width}, H={zone.Height}");
			GenerateFloorForZone(zone);
		}
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
		floor.GenerateAnchors();
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
	
	public void ConnectZonesVertically(Room room)
	{
		foreach (Zone lower in zones)
		{
			foreach (Zone upper in zones)
			{
				// Skip if it's the same zone
				if (lower == upper) continue;

				// Only check zones where the second one is higher up
				if (upper.Y <= lower.Y)
				{
					// Check if there's any horizontal overlap
					int left = Math.Max(lower.X, upper.X);
					int right = Math.Min(lower.X + lower.Width, upper.X + upper.Width);
					int overlapWidth = right - left;

					if (overlapWidth >= 1) // Ensure there’s at least 1 tile of overlap
					{
						int verticalGap = lower.Y - (upper.Y);
						if (verticalGap > 0){
							GD.Print($"🔍 Vertical connection possible from Zone at ({lower.X},{lower.Y}) to ({upper.X},{upper.Y})");
							GD.Print($"➡️ Overlap Width: {overlapWidth}, Vertical Gap: {verticalGap}");

							// Decision logic — for now, just print which primitive would make sense
							if (verticalGap <= 2) {
								GD.Print("📦 Use a platform.");
							}
							else if (verticalGap <= 3) {
								GD.Print("🪜 Use a spring.");
							}
							else if (overlapWidth >= 1) {
								GD.Print("🧗 Use a ladder or slope.");
								Ladder ladder = new Ladder();
								ladder.position = new Vector2(upper.X + overlapWidth , upper.Y + upper.Height - 1);
								ladder.length = verticalGap + 1;
								ladder.GenerateInRoom(room);
								ladder.GenerateAnchors();
							}
							else {
								GD.Print("❌ No vertical connection possible.");
							}
						}
					}
				}
			}
		}
	}

	public void DrawZoneBorders(Room room)
	{
		float tileSize = 70f;

		foreach (Zone zone in zones)
		{
			Line2D border = new Line2D();
			border.DefaultColor = new Color(1, 0, 0); // Red
			border.Width = 2;

			Vector2 topLeft = new Vector2(zone.X, zone.Y) * tileSize;
			Vector2 topRight = new Vector2(zone.X + zone.Width, zone.Y) * tileSize;
			Vector2 bottomRight = new Vector2(zone.X + zone.Width, zone.Y + zone.Height) * tileSize;
			Vector2 bottomLeft = new Vector2(zone.X, zone.Y + zone.Height) * tileSize;

			border.AddPoint(topLeft);
			border.AddPoint(topRight);
			border.AddPoint(bottomRight);
			border.AddPoint(bottomLeft);
			border.AddPoint(topLeft); // Close the rectangle

			room.AddChild(border); // Attach border to room
		}
	}
}
