using Godot;
using System;
using System.Collections.Generic;

public partial class Room : Node2D
{
	public int Width { get; private set; }
	public int Height { get; private set; }
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
	}
	
	public Vector2 GetRandomPosition() {
		Random rng = new Random();
		Vector2 position;
		int attempts = 0;
		const int maxAttempts = 10;  // Prevent infinite loops

		do {
			int x = rng.Next(1, Width - 1);
			int y = rng.Next(1, Height - 1);
			
			position = new Vector2(x * 64, y * 64); // Ensure spacing
			
			attempts++;
		} while (Primitives.Exists(p => p.GlobalPosition == position) && attempts < maxAttempts);

		if (attempts >= maxAttempts) {
			GD.Print($"⚠️ WARNING: Could not find unique placement, skipping.");
			return Vector2.Zero;  // Fail gracefully
		}

		GD.Print($"📍 Unique Position Generated: {position}");
		
		return position;
	}

	public void AddPrimitive(Primitive primitive) {

		if (Primitives.Exists(p => p.GlobalPosition == primitive.GlobalPosition)) {
			GD.Print($"❌ ERROR: Duplicate placement for {primitive.GetType().Name} at {primitive.GlobalPosition}");
			return; // Prevent adding duplicate primitive
		}

		// Validate placement rules before adding
		if (primitive.ValidatePlacement(this)) {
			Primitives.Add(primitive);
			GD.Print($"{primitive.GetType().Name} at {primitive.GlobalPosition}");
			AddChild(primitive);
			GD.Print($"✅ Successfully added {primitive.GetType().Name} at {primitive.GlobalPosition}");
		} else {
			GD.Print($"❌ ERROR: Invalid placement for {primitive.GetType().Name} at {primitive.GlobalPosition}");
		}
	}
	
	public Primitive GetRandomPrimitiveFromCategory(Primitive.PrimitiveCategory category)
	{
		List<Type> matchingPrimitives = new List<Type>();

		foreach (Type primitiveType in PrimitiveRegistry.GetAllPrimitives()) // Get all available primitives
		{
			Primitive tempPrimitive = (Primitive)Activator.CreateInstance(primitiveType);
			if (tempPrimitive.Category == category)
			{
				matchingPrimitives.Add(primitiveType);
			}
		}

		if (matchingPrimitives.Count == 0)
		{
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
