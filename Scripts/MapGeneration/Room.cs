using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Room : Node2D
{
	public int Width { get; private set; }
	public int Height { get; private set; }
	public int DifficultyLevel { get; set; }
	public float DifficultyPercent; // 1 = 10%, 10 = 100%
	
	public PlayerController Player { get; private set; } 
	public ZoneHandler ZoneHandler { get; private set; }
	public PathBuilder PathBuilder { get; private set; }
	public List<Atom> Atoms { get; private set; } = new List<Atom>();
	public List<Primitive> Primitives { get; private set; } = new List<Primitive>();

	public Room() {} // Required default constructor for Godot instantiation

	public Room(int width, int height, int difficulty) {
		Width = width;
		Height = height;
		DifficultyLevel = difficulty;
		DifficultyPercent = (float)DifficultyLevel / 5; // 1 = 20%, 5 = 100%
		ZoneHandler = new ZoneHandler(this);
		PathBuilder = new PathBuilder(this);
	}
	
	public InterestingnessResult Initialize(RoomTemplate template)
	{		
		PackedScene playerScene = GD.Load<PackedScene>("res://Scenes/player.tscn");
		PlayerController player = (PlayerController)playerScene.Instantiate();
		this.Player = player; // set reference here
		
		Node2D playerSpawn = GetTree().Root.FindChild("PlayerSpawn", true, false) as Node2D;
		playerSpawn.AddChild(this.Player);
		
		//GD.Print("🚀 Step 1: Starting zone generation");
		GenerateBorder();

		//GD.Print("🚀 Step 2: Generate zones");
		ZoneHandler.GenerateZones();

		//GD.Print("🚀 Step 3: Connect zones");
		ZoneHandler.ConnectZonesVertically(this);

		//GD.Print("🚀 Step 4: Draw borders");
		ZoneHandler.DrawZoneBorders(this);

		//GD.Print("🚀 Step 5: Detect and fill enclosed areas");
		DetectAndFillEnclosedAreas();
	//
		//GD.Print("🚀 Step 6: Generate Envronmentals");
		GenerateEnvironmentals();
		
		//GD.Print("🚀 Step 7: Generate Hazards");
		GenerateHazards();
		
		//GD.Print("🚀 Step 8: Generate Doors and Locks");
		GenerateDoors();
		
		//GD.Print("🚀 Step 9: Remove Intersecting Anchor Connections");
		AnchorConnector.RemoveIntersectingAnchorConnections(this);
		
		//GD.Print("🚀 Step 10: Modify Floor Tile Behaviour");
		replaceFloorTilesWithTilesAbove();
		
		//GD.Print("🚀 Step 11: Build Path");
		InterestingnessResult result = PathBuilder.GeneratePathsFromStartDoor();
		this.Player.QueueFree();
		
		//GD.Print("🚀 Step 12: Spawn Player");
		SpawnPlayer(); // spawn the player after generating the room
		
		Node2D primitivesContainer = GetTree().Root.FindChild("PrimitivesContainer", true, false) as Node2D;
		primitivesContainer.Position = new Vector2(40, 70);
		
		return result;
	}

	private void SpawnPlayer()
	{
		PackedScene playerScene = GD.Load<PackedScene>("res://Scenes/player.tscn");
		PlayerController player = (PlayerController)playerScene.Instantiate();

		// Place player on a floor tile
		//Atom spawnAtom = Primitives.Find(p => p is Door) as Door;
		List<Door> doors = Primitives.Where(p => p is Door).Cast<Door>().ToList();
		Door startDoor = doors.FirstOrDefault(d => d.isStartDoor);

		if (startDoor != null) {
			player.GlobalPosition = startDoor.GetAtoms().First().GlobalPosition + new Vector2(35, -70);
		} else {
			player.GlobalPosition = doors.First().GetAtoms().First().GlobalPosition + new Vector2(35, -70);
			//GD.Print("⚠️ WARNING: No start door found for player spawn.");
		}

		Node2D playerSpawn = GetTree().Root.FindChild("PlayerSpawn", true, false) as Node2D;
		playerSpawn.AddChild(player);

		player.CurrentRoom = this;
		this.Player = player; // ← Set reference here

		//GD.Print($"✅ Player spawned at {player.GlobalPosition}");
	}
	
	private void replaceFloorTilesWithTilesAbove() {
		
		foreach (Primitive floor in Primitives.Where(p => p.GetType() == typeof(Floor))) {
			List<Atom> tiles  = floor.GetAtoms();
			tiles.Sort((a, b) => a.GlobalPosition.X.CompareTo(b.GlobalPosition.X));
			foreach (Atom tile in tiles) {
				if (!HasAtomAt(tile.GlobalPosition - new Vector2(70, 0))
					|| HasAtomOfTypeAt(tile.GlobalPosition - new Vector2(70, 0), typeof(TopWaterTile))) { // no tile on left
					if (!HasAtomAt(tile.GlobalPosition + new Vector2(70, 0))
						|| HasAtomOfTypeAt(tile.GlobalPosition + new Vector2(70, 0), typeof(TopWaterTile))) {
						// middle tile
						tile.SetTexture((Texture2D)GD.Load("res://Assets/kenney_platformer-art-deluxe/Base pack/Tiles/grass.png")); 
					} else {
						// left tile
						tile.SetTexture((Texture2D)GD.Load("res://Assets/kenney_platformer-art-deluxe/Base pack/Tiles/grassLeft.png")); 
					}
				} else if (!HasAtomAt(tile.GlobalPosition + new Vector2(70, 0))
					|| HasAtomOfTypeAt(tile.GlobalPosition + new Vector2(70, 0), typeof(TopWaterTile))) { // no tile on right
					// right tile
					tile.SetTexture((Texture2D)GD.Load("res://Assets/kenney_platformer-art-deluxe/Base pack/Tiles/grassRight.png")); 
				}
			}
		}
		
		foreach(Floor floor in Primitives.Where(p => p is Floor).Cast<Floor>().ToList()) {
			floor.GenerateSideAnchors(this);
		}
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
	
	public void DetectAndFillEnclosedAreas() {
		bool addedWall = true;

		while (addedWall) {
			List<(Vector2 start, Vector2 end)> obstructionLines = new List<(Vector2, Vector2)>();

			foreach (Primitive primitive in Primitives) {
				obstructionLines.AddRange(primitive.ObstructionLines);
			}

			// Before rectangle detection
			obstructionLines = MergeColinearLines(obstructionLines);
			
			List<(Vector2 start, Vector2 end)> verticals = obstructionLines.Where(line => Math.Abs(line.start.X - line.end.X) < 1f).ToList();
			List<(Vector2 start, Vector2 end)> horizontals = obstructionLines.Where(line => Math.Abs(line.start.Y - line.end.Y) < 1f).ToList();

			HashSet<string> visitedRectangles = new HashSet<string>();

			addedWall = false;
			
			for (int i = 0; i < verticals.Count; i++) {
				for (int j = i + 1; j < verticals.Count; j++) {
					var v1 = verticals[i];
					var v2 = verticals[j];

					if (v1.start.X > v2.start.X) {
						var temp = v1;
						v1 = v2;
						v2 = temp;
					}

					for (int k = 0; k < horizontals.Count; k++) {
						for (int l = k + 1; l < horizontals.Count; l++) {
							var h1 = horizontals[k];
							var h2 = horizontals[l];

							if (h1.start.Y > h2.start.Y) {
								var temp = h1;
								h1 = h2;
								h2 = temp;
							}

							bool hasTop = LineCovers(h1, v1.start.X) && LineCovers(h1, v2.start.X);
							bool hasBottom = LineCovers(h2, v1.start.X) && LineCovers(h2, v2.start.X);
							bool hasLeft = LineCovers(v1, h1.start.Y) && LineCovers(v1, h2.start.Y);
							bool hasRight = LineCovers(v2, h1.start.Y) && LineCovers(v2, h2.start.Y);

							if (hasTop && hasBottom && hasLeft && hasRight) {
								Vector2 topLeft = new Vector2(Math.Min(v1.start.X, v2.start.X), Math.Min(h1.start.Y, h2.start.Y));
								Vector2 bottomRight = new Vector2(Math.Max(v1.start.X, v2.start.X), Math.Max(h1.start.Y, h2.start.Y));

								if (topLeft.X == bottomRight.X || topLeft.Y == bottomRight.Y)
									continue;

								// Create a unique key for the rectangle
								string rectKey = $"{topLeft}-{bottomRight}";
								if (visitedRectangles.Contains(rectKey))
									continue;

								visitedRectangles.Add(rectKey);

								//GD.Print($"🟥 Rectangle found between {topLeft} and {bottomRight}");

								if (FillRectangleWithWall(topLeft + new Vector2(35, 35), bottomRight + new Vector2(35, 35))) {
									addedWall = true;
								}
							}
						}
					}
				}
			}
		}
	}

	private List<(Vector2 start, Vector2 end)> MergeColinearLines(List<(Vector2 start, Vector2 end)> lines) {
		List<(Vector2 start, Vector2 end)> merged = new List<(Vector2 start, Vector2 end)>();
		bool[] used = new bool[lines.Count];

		for (int i = 0; i < lines.Count; i++) {
			if (used[i]) continue;

			var (start1, end1) = lines[i];
			Vector2 mergedStart = start1;
			Vector2 mergedEnd = end1;

			bool mergedAny;
			do {
				mergedAny = false;

				for (int j = 0; j < lines.Count; j++) {
					if (i == j || used[j]) continue;

					var (start2, end2) = lines[j];

					// Normalize direction
					if (start2.X > end2.X || start2.Y > end2.Y) {
						(start2, end2) = (end2, start2);
					}
					if (mergedStart.X > mergedEnd.X || mergedStart.Y > mergedEnd.Y) {
						(mergedStart, mergedEnd) = (mergedEnd, mergedStart);
					}

					// Horizontal merge (same Y, touching)
					if (Mathf.Abs(mergedStart.Y - start2.Y) < 1f && Mathf.Abs(mergedEnd.Y - end2.Y) < 1f) {
						if (mergedEnd.X == start2.X) {
							mergedEnd = end2;
							used[j] = true;
							mergedAny = true;
							break;
						} else if (mergedStart.X == end2.X) {
							mergedStart = start2;
							used[j] = true;
							mergedAny = true;
							break;
						}
					} else if (Mathf.Abs(mergedStart.X - start2.X) < 1f && Mathf.Abs(mergedEnd.X - end2.X) < 1f) { // Vertical merge (same X, touching)
						if (mergedEnd.Y == start2.Y) {
							mergedEnd = end2;
							used[j] = true;
							mergedAny = true;
							break;
						} else if (mergedStart.Y == end2.Y) {
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

	private bool LineCovers((Vector2 start, Vector2 end) line, float value) {
		if (Math.Abs(line.start.Y - line.end.Y) < 1f) { // horizontal
			return value >= Math.Min(line.start.X, line.end.X) && value <= Math.Max(line.start.X, line.end.X);
		} else { // vertical
			return value >= Math.Min(line.start.Y, line.end.Y) && value <= Math.Max(line.start.Y, line.end.Y);
		}
	}

	private bool FillRectangleWithWall(Vector2 topLeft, Vector2 bottomRight)
	{
		Wall wall = new Wall();
		wall.Position = new Vector2(topLeft.X, topLeft.Y);
		wall.width = (int)MathF.Round((bottomRight.X - topLeft.X) / 70); // assuming 70px tile
		wall.height = (int)MathF.Round((bottomRight.Y - topLeft.Y) / 70);

		if (wall.width > 0 && wall.height > 0)
		{
			//GD.Print($"✅ Placing Wall at {wall.Position} with size {wall.width}x{wall.height}");
			if (wall.GenerateInRoom(this)) {
				return true;
			}
		}
		return false;
	}
	
	public void GenerateHazards()
	{
		List<Type> hazardTypes = new List<Type> { typeof(FloorBlade), typeof(FullBlade), typeof(Fish), typeof(Slug) };
		List<Vector2> validPositions = GetPositionsAboveFloorTiles();

		// Scale min and max with difficulty
		Random random = new Random();

		float roomArea = Width * 30;
		float baseDensity = 0.0045f; // Tunable: hazards per tile at 100% difficulty
		float density = baseDensity * DifficultyPercent;

		int expectedHazards = Mathf.RoundToInt(roomArea * density);

		// Add variability
		int variation = (int)(expectedHazards * 0.2f); // ±20% variation
		int minHazards = Math.Max(1, expectedHazards - variation);
		int maxHazards = expectedHazards + variation;

		int noOfHazards = random.Next(minHazards, maxHazards + 1);
		
		
		//int noOfHazards = random.Next(1, 15);

		for (int i = 0; i < noOfHazards; i++) {
			if (hazardTypes.Count == 0 ) {
				//GD.Print("⚠️ No hazard types remaining.");
				break;
			}

			if (validPositions.Count == 0) {
				//GD.Print("⚠️ No more valid positions left for hazards.");
				break;
			}

			// Pick a hazard type
			Type hazardType = hazardTypes[random.Next(hazardTypes.Count)];
			int index = random.Next(validPositions.Count);
			Vector2 chosenPosition = validPositions[index];
			validPositions.RemoveAt(index);

			Primitive hazard = (Primitive)Activator.CreateInstance(hazardType);
			hazard.Position = chosenPosition;

			if (!hazard.GenerateInRoom(this)) {
				//GD.Print($"❌ Failed to place {hazardType.Name} at {chosenPosition}. Trying another...");
				i--; // Retry
			}
		}
	}
	
	public void GenerateEnvironmentals()
	{
		List<Type> environmentalTypes = new List<Type> { typeof(Water), typeof(Pit) };
		List<Vector2> validPositions = GetPositionsAboveFloorTiles();

		// Scale min and max with difficulty
		Random random = new Random();

		float roomArea = Width * 30;
		float baseDensity = 0.01f;
		float exponent = 0.75f;
		float baseCount = Mathf.Max(0, Mathf.Pow(roomArea, exponent) * baseDensity * DifficultyPercent); // clamp to ≥0

		int expected = Mathf.RoundToInt(baseCount);
		int variation = Math.Max(1, expected / 2);

		int min = expected - variation;
		int max = expected + variation;

		// ✅ FINAL SAFETY CHECK
		min = Math.Max(1, Math.Min(min, max));
		max = Math.Max(min, max);

		int noOfEnvironmentals = random.Next(min, max + 1);
		
		//int noOfEnvironmentals = random.Next(3, 15);
		
		for (int i = 0; i < noOfEnvironmentals; i++) {
			if (environmentalTypes.Count == 0) {
				//GD.Print("⚠️ No environmental types remaining.");
				break;
			}

			if (validPositions.Count == 0) {
				//GD.Print("⚠️ No more valid positions left for environmentals.");
				break;
			}

			// Pick a hazard type
			Type environmentalType = environmentalTypes[random.Next(environmentalTypes.Count)];

			if (environmentalType == typeof(Water)) {
				Water water = new Water();

				// Let Pit find its own valid placement
				bool success = water.GenerateInRoom(this);
				if (success) {
					// Recompute valid floor tile positions after modifying the room
					validPositions = GetPositionsAboveFloorTiles();
				} else {
					GD.Print("🚫 No valid spot for water. Removing it from env list.");
					environmentalTypes.Remove(environmentalType);
					i--; // Retry the current iteration
				}

				continue; // Skip the rest of the loop for pit
			} else if (environmentalType == typeof(Pit)) {
				Pit pit = new Pit();
				if (pit.GenerateInRoom(this)) { // Recompute valid floor tile positions after modifying the room
					validPositions = GetPositionsAboveFloorTiles();
				} else {
					GD.Print("🚫 No valid spot for pit. Removing it from env list.");
					environmentalTypes.Remove(environmentalType);
					i--; // Retry the current iteration
				}

				continue; // Skip the rest of the loop for pit
			}

			// For non-pit envs
			int index = random.Next(validPositions.Count);
			Vector2 chosenPosition = validPositions[index];
			validPositions.RemoveAt(index);

			Primitive environmental = (Primitive)Activator.CreateInstance(environmentalType);
			environmental.Position = chosenPosition;

			if (!environmental.GenerateInRoom(this)) {
				//GD.Print($"❌ Failed to place {environmentalType.Name} at {chosenPosition}. Trying another...");
				i--; // Retry
			}
		}
	}
	
	public void GenerateDoors()
	{
		List<Pit> pitPrimitives = Primitives.Where(p => p is Pit).Cast<Pit>().OrderByDescending(p => p.Depth).ThenByDescending(p => p.Width).ToList();
		List<Water> waterPrimitives = Primitives.Where(p => p is Water).Cast<Water>().OrderByDescending(p => p.Depth).ThenByDescending(p => p.Width).ToList();
		
		//GD.Print($"🌊 Water count: {waterPrimitives.Count}, 🕳️ Pit count: {pitPrimitives.Count}");
		
		List<Vector2> validPositions = this.GetPositionsAboveFloorTiles();
		Random random = new Random();

		int noOfDoors = random.Next(2, 5); // max 4 doors

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

			while (HasAtomAt(door.Position) || HasAtomAt(door.Position - new Vector2(0, 70)) || HasAtomAt(doorLock.Position))
			{
				//GD.Print($"Cannot place door at ({validPositions[index].X}, {validPositions[index].Y})");

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
			
			door.GenerateInRoom(this);
			doorLock.GenerateInRoom(this);
			
			// Now spawn the matching key at a random floor tile
			if (validPositions.Count == 0) break;
		}
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
			//primitivesContainer.AddChild(atom); // Add atoms to the correct container
			return true;
		} else {
			//GD.Print($"❌ ERROR: Invalid placement for {atom.GetType().Name} at {atom.GlobalPosition}");
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
				//GD.Print($"❌ ERROR: Overlapping atom detected for {primitive.GetType().Name} at {atom.GlobalPosition}");
				primitive.Free();
				return false; // Prevent adding overlapping atoms
			}

			if (HasAtomAt(atom.GlobalPosition))
			{
				//GD.Print($"❌ ERROR: Overlapping atom detected for {primitive.GetType().Name} at {atom.GlobalPosition}");
				primitive.Free();
				return false; // Prevent adding overlapping atoms
			}
			
			// Validate placement rules before adding the atom
			if (!atom.ValidatePlacement(this))
			{
				//GD.Print($"❌ ERROR: Invalid placement for {atom.GetType().Name} at {atom.GlobalPosition}");
				primitive.Free();
				return false;
			}
		} 
		
		foreach (Atom atom in primitive.GetAtoms()) {
			if (atom is FloorBladeAtom || atom is SlugAtom) {
				atom.GlobalPosition += new Vector2(0, 20);
			} else if (atom is LockAtom) {
				atom.GlobalPosition += new Vector2(40, 70);
			}
			this.AddAtom(atom);
		}

		// If all atoms pass validation, add the primitive
		primitive.GenerateAnchors(this);
		Primitives.Add(primitive);		
		Node2D primitivesContainer = GetTree().Root.FindChild("PrimitivesContainer", true, false) as Node2D;
		primitivesContainer.AddChild(primitive); // Add atoms to the correct container
		//GD.Print($"✅ Added {primitive.GetType().Name} to PrimitivesContainer at {primitive.Position}");
		return true;
	}
	
	public void RemovePrimitive(Primitive primitive)
	{
		if (Primitives.Contains(primitive))
		{
			Primitives.Remove(primitive);
			RemoveChild(primitive);
			primitive.QueueFree();
			//GD.Print($"🗑️ Removed {primitive.GetType().Name} from room.");
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
			//GD.Print($"⚠️ WARNING: No primitives found for category {category}");
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
			return Atoms.Exists(a => a.GlobalPosition == position || a.GlobalPosition - new Vector2(0, 20) == position || a.GlobalPosition - new Vector2(40, 70) == position);
	}
	
	public bool HasAtomOfTypeAt(Vector2 position, Type atomType) {
			return Atoms.Exists(a => a.GetType() == atomType && a.GlobalPosition == position);
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
	
	public void RemoveAnchorsAt(Vector2 globalPosition)
	{
		foreach (Primitive primitive in Primitives)
		{
			// Remove anchors from the primitive that are too close to the removed tile
			primitive.Anchors.RemoveAll(anchor => anchor.Position.DistanceTo(globalPosition) < 5); // 5 pixels tolerance
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
