using Godot;
using System;
using System.Collections.Generic;

public partial class Room : Node2D
{
	public int Width { get; private set; }
	public int Height { get; private set; }
	public List<Atom> Atoms { get; private set; } = new List<Atom>();
	public List<Primitive> Primitives { get; private set; } = new List<Primitive>();

	public Room() {} // Required default constructor for Godot instantiation

	public Room(int width, int height) {
		Width = width;
		Height = height;
	}
	
	public void Initialize(RoomTemplate template)
	{
		GD.Print($"Initializing {template.Type} room...");
		
		foreach (Primitive.PrimitiveCategory category in template.RequiredPrimitiveCategories) {
			Primitive chosenPrimitive = GetRandomPrimitiveFromCategory(category);
			if (chosenPrimitive != null) { chosenPrimitive.GenerateInRoom(this); }
		}
		
		AnchorConnector.ExpandRoomFromAnchors(this, 10);
		SpawnPlayer(); // spawn the player after generating the room
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
	
	public void AddAtom(Atom atom) {
		// Prevent duplicate placement of atoms
		if (Primitives.Exists(p => p.GlobalPosition == atom.GlobalPosition)) {
			GD.Print($"❌ ERROR: Duplicate placement for {atom.GetType().Name} at {atom.GlobalPosition}");
			return;
		}

		// Validate placement rules before adding
		if (atom.ValidatePlacement(this)) {
			Atoms.Add(atom);
			Node2D primitivesContainer = GetTree().Root.FindChild("PrimitivesContainer", true, false) as Node2D;
			primitivesContainer.AddChild(atom); // Add atoms to the correct container
			GD.Print($"✅ Added {atom.GetType().Name} to PrimitivesContainer at {atom.GlobalPosition}");
		} else {
			GD.Print($"❌ ERROR: Invalid placement for {atom.GetType().Name} at {atom.GlobalPosition}");
		}
	}
	
	public void AddPrimitive(Primitive primitive)
	{
		foreach (Atom atom in primitive.GetAtoms())
		{
			if (Primitives.Exists(p => p.GetAtoms().Exists(a => a.GlobalPosition == atom.GlobalPosition)))
			{
				GD.Print($"❌ ERROR: Overlapping atom detected for {primitive.GetType().Name} at {atom.GlobalPosition}");
				return; // Prevent adding overlapping atoms
			}

			// Validate placement rules before adding the atom
			if (!atom.ValidatePlacement(this))
			{
				GD.Print($"❌ ERROR: Invalid placement for {atom.GetType().Name} at {atom.GlobalPosition}");
				return;
			}
		}

		// If all atoms pass validation, add the primitive
		primitive.GenerateAnchors();
		Primitives.Add(primitive);		
		Node2D primitivesContainer = GetTree().Root.FindChild("PrimitivesContainer", true, false) as Node2D;
		primitivesContainer.AddChild(primitive); // Add atoms to the correct container
		GD.Print($"✅ Added {primitive.GetType().Name} to PrimitivesContainer at {primitive.GlobalPosition}");
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
			return Primitives.Exists(p => p.GetType() == primitiveType && p.GlobalPosition == position + new Vector2(0, -70));
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
}
