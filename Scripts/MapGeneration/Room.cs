using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Room : Node2D
{
	public int Width { get; private set; }
	public int Height { get; private set; }
	public ZoneHandler ZoneHandler { get; private set; }
	public PathBuilder PathBuilder { get; private set; }
	public List<Atom> Atoms { get; private set; } = new List<Atom>();
	public List<Primitive> Primitives { get; private set; } = new List<Primitive>();

	public Room() {} // Required default constructor for Godot instantiation

	public Room(int width, int height) {
		Width = width;
		Height = height;
		ZoneHandler = new ZoneHandler(this);
		PathBuilder = new PathBuilder(this);
	}
	
	public void Initialize(RoomTemplate template)
	{
		GD.Print($"Initializing {template.Type} room...");
		
		//foreach (Primitive.PrimitiveCategory category in template.RequiredPrimitiveCategories) {
			//Primitive chosenPrimitive = GetRandomPrimitiveFromCategory(category);
			//if (chosenPrimitive != null) { chosenPrimitive.GenerateInRoom(this); }
		//}
		//
		//// generate anchors AFTER all primitives are placed
		//foreach (Primitive p in Primitives) { p.GenerateAnchors(); }
		GenerateBorder();
		ZoneHandler.GenerateZones();
		ZoneHandler.ConnectZonesVertically(this);
		ZoneHandler.DrawZoneBorders(this);
		AnchorConnector.RemoveIntersectingAnchorConnections(this);
		DetectAndFillEnclosedAreas();
		GenerateDoors();
		PathBuilder.GenerateHazards();
		PathBuilder.BuildPathsBetweenDoors(this);

		SpawnPlayer(); // spawn the player after generating the room
		Node2D primitivesContainer = GetTree().Root.FindChild("PrimitivesContainer", true, false) as Node2D;
		primitivesContainer.Position = new Vector2(40, 70);

	}
	
	private void SpawnPlayer() {
		PackedScene playerScene = GD.Load<PackedScene>("res://Scenes/player.tscn");
		PlayerController player = (PlayerController)playerScene.Instantiate();

		// Find the player spawn point (e.g., first floor tile)
		Atom spawnAtom = Atoms.Find(p => p is FloorTile);
		if (spawnAtom != null) {
			player.GlobalPosition = spawnAtom.GlobalPosition + new Vector2(200,-140); // Offset above the floor
		} else {
			GD.Print("⚠️ WARNING: No valid floor found for player spawn. Defaulting to (0,0)");
			player.GlobalPosition = new Vector2(0, 0);
		}
		
		Node2D playerSpawn = GetTree().Root.FindChild("PlayerSpawn", true, false) as Node2D;
		playerSpawn.AddChild(player); // Add atoms to the correct container
		GD.Print($"✅ Player spawned at {player.GlobalPosition}");
	}
	
	public void GenerateBorder() {
		int borderThickness = 10;
		// left wall
		Wall leftWall = new Wall();
		leftWall.Position = new Vector2(-borderThickness, 0) * new Vector2(70, 70);
		leftWall.width = borderThickness;
		leftWall.height = this.Height;
		leftWall.GenerateInRoom(this);
		
		// right wall
		Wall rightWall = new Wall();
		rightWall.Position = new Vector2(this.Width, 0) * new Vector2(70, 70);
		rightWall.width = borderThickness;
		rightWall.height = this.Height;
		rightWall.GenerateInRoom(this);
		
		// floor
		Wall floor = new Wall();
		floor.Position = new Vector2(-borderThickness, this.Height) * new Vector2(70, 70);
		floor.height = borderThickness;
		floor.width = this.Width + (borderThickness * 2);
		floor.GenerateInRoom(this);
		
		// ceiling
		Wall ceiling = new Wall();
		ceiling.Position = new Vector2(- borderThickness, -borderThickness) * new Vector2(70, 70);
		ceiling.height = borderThickness;
		ceiling.width = this.Width + (borderThickness * 2);
		ceiling.GenerateInRoom(this);
	}
	
	public void DetectAndFillEnclosedAreas()
	{
		List<(Vector2 start, Vector2 end)> obstructionLines = new List<(Vector2, Vector2)>();

		foreach (Primitive primitive in Primitives)
		{
			obstructionLines.AddRange(primitive.ObstructionLines);
		}

		// Before rectangle detection
		obstructionLines = MergeColinearLines(obstructionLines);
		
		List<(Vector2 start, Vector2 end)> verticals = obstructionLines.Where(line => Math.Abs(line.start.X - line.end.X) < 1f).ToList();
		List<(Vector2 start, Vector2 end)> horizontals = obstructionLines.Where(line => Math.Abs(line.start.Y - line.end.Y) < 1f).ToList();

		HashSet<string> visitedRectangles = new HashSet<string>();

		for (int i = 0; i < verticals.Count; i++)
		{
			for (int j = i + 1; j < verticals.Count; j++)
			{
				var v1 = verticals[i];
				var v2 = verticals[j];

				if (v1.start.X > v2.start.X)
				{
					var temp = v1;
					v1 = v2;
					v2 = temp;
				}

				for (int k = 0; k < horizontals.Count; k++)
				{
					for (int l = k + 1; l < horizontals.Count; l++)
					{
						var h1 = horizontals[k];
						var h2 = horizontals[l];

						if (h1.start.Y > h2.start.Y)
						{
							var temp = h1;
							h1 = h2;
							h2 = temp;
						}

						bool hasTop = LineCovers(h1, v1.start.X) && LineCovers(h1, v2.start.X);
						bool hasBottom = LineCovers(h2, v1.start.X) && LineCovers(h2, v2.start.X);
						bool hasLeft = LineCovers(v1, h1.start.Y) && LineCovers(v1, h2.start.Y);
						bool hasRight = LineCovers(v2, h1.start.Y) && LineCovers(v2, h2.start.Y);

						if (hasTop && hasBottom && hasLeft && hasRight)
						{
							Vector2 topLeft = new Vector2(Math.Min(v1.start.X, v2.start.X), Math.Min(h1.start.Y, h2.start.Y));
							Vector2 bottomRight = new Vector2(Math.Max(v1.start.X, v2.start.X), Math.Max(h1.start.Y, h2.start.Y));

							if (topLeft.X == bottomRight.X || topLeft.Y == bottomRight.Y)
								continue;

							// Create a unique key for the rectangle
							string rectKey = $"{topLeft}-{bottomRight}";
							if (visitedRectangles.Contains(rectKey))
								continue;

							visitedRectangles.Add(rectKey);

							GD.Print($"🟥 Rectangle found between {topLeft} and {bottomRight}");

							FillRectangleWithWall(topLeft + new Vector2(35, 35), bottomRight + new Vector2(35, 35));
						}
					}
				}
			}
		}
	}

	private List<(Vector2 start, Vector2 end)> MergeColinearLines(List<(Vector2 start, Vector2 end)> lines)
	{
		List<(Vector2 start, Vector2 end)> merged = new List<(Vector2 start, Vector2 end)>();
		bool[] used = new bool[lines.Count];

		for (int i = 0; i < lines.Count; i++)
		{
			if (used[i]) continue;

			var (start1, end1) = lines[i];
			Vector2 mergedStart = start1;
			Vector2 mergedEnd = end1;

			bool mergedAny;
			do
			{
				mergedAny = false;

				for (int j = 0; j < lines.Count; j++)
				{
					if (i == j || used[j]) continue;

					var (start2, end2) = lines[j];

					// Normalize direction
					if (start2.X > end2.X || start2.Y > end2.Y)
					{
						(start2, end2) = (end2, start2);
					}
					if (mergedStart.X > mergedEnd.X || mergedStart.Y > mergedEnd.Y)
					{
						(mergedStart, mergedEnd) = (mergedEnd, mergedStart);
					}

					// Horizontal merge (same Y, touching)
					if (Mathf.Abs(mergedStart.Y - start2.Y) < 1f && Mathf.Abs(mergedEnd.Y - end2.Y) < 1f)
					{
						if (mergedEnd.X == start2.X)
						{
							mergedEnd = end2;
							used[j] = true;
							mergedAny = true;
							break;
						}
						else if (mergedStart.X == end2.X)
						{
							mergedStart = start2;
							used[j] = true;
							mergedAny = true;
							break;
						}
					}
					// Vertical merge (same X, touching)
					else if (Mathf.Abs(mergedStart.X - start2.X) < 1f && Mathf.Abs(mergedEnd.X - end2.X) < 1f)
					{
						if (mergedEnd.Y == start2.Y)
						{
							mergedEnd = end2;
							used[j] = true;
							mergedAny = true;
							break;
						}
						else if (mergedStart.Y == end2.Y)
						{
							mergedStart = start2;
							used[j] = true;
							mergedAny = true;
							break;
						}
					}
				}
			}
			while (mergedAny);

			merged.Add((mergedStart, mergedEnd));
		}

		return merged;
	}

	private bool LineCovers((Vector2 start, Vector2 end) line, float value)
	{
		if (Math.Abs(line.start.Y - line.end.Y) < 1f) // horizontal
		{
			return value >= Math.Min(line.start.X, line.end.X) && value <= Math.Max(line.start.X, line.end.X);
		}
		else // vertical
		{
			return value >= Math.Min(line.start.Y, line.end.Y) && value <= Math.Max(line.start.Y, line.end.Y);
		}
	}

	private void FillRectangleWithWall(Vector2 topLeft, Vector2 bottomRight)
	{
		Wall wall = new Wall();
		wall.Position = new Vector2(topLeft.X, topLeft.Y);
		wall.width = (int)MathF.Round((bottomRight.X - topLeft.X) / 70); // assuming 70px tile
		wall.height = (int)MathF.Round((bottomRight.Y - topLeft.Y) / 70);

		if (wall.width > 0 && wall.height > 0) {
			GD.Print($"✅ Placing Wall at {wall.Position} with size {wall.width}x{wall.height}");
			wall.GenerateInRoom(this);
		}
	}
	
	public void GenerateDoors()
	{
		List<Vector2> validPositions = this.GetPositionsAboveFloorTiles();
		Random random = new Random();

		int noOfDoors = random.Next(1, 5); // max 4 doors
		List<DoorColour> availableColors = Enum.GetValues(typeof(DoorColour)).Cast<DoorColour>().ToList();

		// Shuffle colors
		availableColors = availableColors.OrderBy(_ => random.Next()).ToList();

		for (int i = 0; i < noOfDoors && i < availableColors.Count; i++)
		{
			Door door = new Door();
			door.Colour = availableColors[i]; // Assign a unique color

			int index = random.Next(validPositions.Count);
			door.Position = validPositions[index];

			DoorLock doorLock = new DoorLock();
			doorLock.Colour = door.Colour;
			doorLock.Position = door.Position + new Vector2(70, 0);

			validPositions.RemoveAt(index);

			while (!door.GenerateInRoom(this) || !doorLock.GenerateInRoom(this))
			{
				GD.Print($"Cannot place door at ({validPositions[index].X}, {validPositions[index].Y})");

				if (validPositions.Count == 0) break;

				door = new Door();
				door.Colour = availableColors[i];

				index = random.Next(validPositions.Count);
				door.Position = validPositions[index];

				doorLock = new DoorLock();
				doorLock.Colour = door.Colour;
				doorLock.Position = door.Position + new Vector2(70, 0);

				validPositions.RemoveAt(index);
			}

			// Now spawn the matching key at a random floor tile
			if (validPositions.Count == 0) break;

			bool keyPlaced = false;
			while (!keyPlaced && validPositions.Count > 0)
			{
				int keyIndex = random.Next(validPositions.Count);
				Vector2 keyPosition = validPositions[keyIndex];
				validPositions.RemoveAt(keyIndex);

				DoorKey key = new DoorKey
				{
					Colour = door.Colour,
					Position = keyPosition 
				};

				if (key.GenerateInRoom(this))
				{
					GD.Print($"🗝️ Placed key at {key.Position} for colour {key.Colour}");
					keyPlaced = true;
				}
			}
		}
	}
	
	public Vector2 GetRandomPosition() {
		Random rng = new Random();
		Vector2 position;
		int attempts = 0;
		const int maxAttempts = 10;  // Prevent infinite loops

		do {
			int x = rng.Next(1, Width - 1);
			int y = rng.Next(-Height + 1, 0);
			
			position = new Vector2(x * 70, y * 70); // Ensure spacing
			
			attempts++;
		} while (Primitives.Exists(p => p.GlobalPosition == position) && attempts < maxAttempts);

		if (attempts >= maxAttempts) {
			GD.Print($"⚠️ WARNING: Could not find unique placement, skipping.");
			return Vector2.Zero;  // Fail gracefully
		}

		GD.Print($"📍 Unique Position Generated: {position}");
		
		return position;
	}

	public List<Anchor> GetAllAnchors()
	{
		List<Anchor> anchors = new List<Anchor>();
		
		foreach (Primitive primitive in Primitives)
		{
			anchors.AddRange(primitive.Anchors);
		}

		return anchors;
	}
	
	public bool AddAtom(Atom atom) {		
		// Validate placement rules before adding
		if (atom.ValidatePlacement(this)) {
			Atoms.Add(atom);
			Node2D primitivesContainer = GetTree().Root.FindChild("PrimitivesContainer", true, false) as Node2D;
			primitivesContainer.AddChild(atom); // Add atoms to the correct container
			return true;
			//GD.Print($"✅ Added {atom.GetType().Name} to PrimitivesContainer at {atom.GlobalPosition}");
		} else {
			GD.Print($"❌ ERROR: Invalid placement for {atom.GetType().Name} at {atom.GlobalPosition}");
			return false;
		}
		return true;
	}
	
	public bool AddPrimitive(Primitive primitive)
	{
		foreach (Atom atom in primitive.GetAtoms())
		{ 		// Prevent duplicate placement of atoms
			if (Primitives.Exists(p => p.GetAtoms().Exists(a => a.GlobalPosition == atom.GlobalPosition)))
			{
				GD.Print($"❌ ERROR: Overlapping atom detected for {primitive.GetType().Name} at {atom.GlobalPosition}");
				return false; // Prevent adding overlapping atoms
			}

			if (Atoms.Exists(a => a.GlobalPosition == atom.GlobalPosition))
			{
				GD.Print($"❌ ERROR: Overlapping atom detected for {primitive.GetType().Name} at {atom.GlobalPosition}");
				return false; // Prevent adding overlapping atoms
			}
			
			// Validate placement rules before adding the atom
			if (!atom.ValidatePlacement(this))
			{
				GD.Print($"❌ ERROR: Invalid placement for {atom.GetType().Name} at {atom.GlobalPosition}");
				return false;
			}
		} 
		
		foreach (Atom atom in primitive.GetAtoms()) {
			if (atom is FloorBladeAtom) {
				atom.GlobalPosition += new Vector2(0, 20);
			}
			this.AddAtom(atom);
		}

		// If all atoms pass validation, add the primitive
		primitive.GenerateAnchors(this);
		Primitives.Add(primitive);		
		Node2D primitivesContainer = GetTree().Root.FindChild("PrimitivesContainer", true, false) as Node2D;
		primitivesContainer.AddChild(primitive); // Add atoms to the correct container
		GD.Print($"✅ Added {primitive.GetType().Name} to PrimitivesContainer at {primitive.Position}");
		return true;
	}
	
	public void RemovePrimitive(Primitive primitive)
	{
		if (Primitives.Contains(primitive))
		{
			Primitives.Remove(primitive);
			RemoveChild(primitive);
			GD.Print($"🗑️ Removed {primitive.GetType().Name} from room.");
		}
		else
		{
			GD.Print($"⚠️ Tried to remove {primitive.GetType().Name} but it was not found.");
		}
	}
	
	public Primitive GetRandomPrimitiveFromCategory(Primitive.PrimitiveCategory category)
	{
		List<Type> matchingPrimitives = new List<Type>();

		foreach (Type primitiveType in PrimitiveRegistry.GetAllPrimitives()) // Get all available primitives
		{
			Primitive tempPrimitive = (Primitive)Activator.CreateInstance(primitiveType);
			if (tempPrimitive.Category == category) {
				matchingPrimitives.Add(primitiveType);
			}
		}

		if (matchingPrimitives.Count == 0) {
			GD.Print($"⚠️ WARNING: No primitives found for category {category}");
			return null;
		}

		Random random = new Random();
		Type chosenType = matchingPrimitives[random.Next(matchingPrimitives.Count)];
		return (Primitive)Activator.CreateInstance(chosenType);
	}

	public bool HasPrimitiveBelow(Vector2 position, Type primitiveType) {
			return Primitives.Exists(p => p.GetType() == primitiveType && p.GlobalPosition == position + new Vector2(0, 70));
	}

	public bool HasPrimitiveAt(Vector2 position) {
			return Primitives.Exists(p => p.GlobalPosition == position);
	}
	
	public bool HasAtomBelow(Vector2 position, Type atomType) {
			return Atoms.Exists(a => a.GetType() == atomType && a.GlobalPosition == position + new Vector2(0, 70));
	}
	
	public bool HasAtomAt(Vector2 position) {
			return Atoms.Exists(a => a.GlobalPosition == position);
	}
	
	public bool HasPlatformNearby(Vector2 position) {
		return Primitives.Exists(p => 
			p.GlobalPosition == position + new Vector2(-70, 0) || 
			p.GlobalPosition == position + new Vector2(70, 0));
	}
	
	public List<Vector2> GetPositionsAboveFloorTiles() {
		List<Vector2> validPositions = new List<Vector2>();

		foreach (Primitive primitive in Primitives) {
			if (primitive is Floor floor) {
				foreach (Node child in floor.GetChildren()) { // Loop through all FloorTiles
					if (child is FloorTile floorTile) {
						Vector2 aboveFloor = floorTile.GlobalPosition + new Vector2(0, -70);
						validPositions.Add(aboveFloor);
					}
				}
			}
		}
		return validPositions;
	}
	
	public List<(Vector2, Vector2)> DebugPathLines = new List<(Vector2, Vector2)>();

	public override void _Draw()
	{
		foreach (var (start, end) in DebugPathLines)
		{
			DrawLine(start + new Vector2(40, 60), end + new Vector2(40, 60), Colors.Blue, 6f);
		}
	}
}
