using Godot;
using System;
using System.Collections.Generic;

public partial class MapGenerator : Node
{
	private Random random = new Random();
	private List<RoomTemplate> roomTemplates = new List<RoomTemplate>();

	public override void _Ready()
	{
		GD.Print("MapGenerator started! Generating map..."); // Debug log
		LoadTemplates();
		GenerateMap();
	}

	private void LoadTemplates() {
		roomTemplates.Add(new RoomTemplate(RoomType.Challenge, 
			new List<Primitive.PrimitiveCategory> { 
				Primitive.PrimitiveCategory.Floor, 
				Primitive.PrimitiveCategory.Hazard, 
				Primitive.PrimitiveCategory.Platform,
				Primitive.PrimitiveCategory.MovementModifier 
			}
		));
	}

	private void GenerateMap()
	{
		Node2D roomsContainer = GetNode<Node2D>("../RoomsContainer"); // Get container
		
		//Room room = (Room)GD.Load<PackedScene>("res://Scenes/Rooms/Room.tscn").Instantiate();
		Room room = new Room(40, 26); // Set Width = 10, Height = 6
		roomsContainer.AddChild(room); // Adds the room dynamically
		//room.Position = new Vector2(400, 200);  // Adjust for centering

		GD.Print($"Generated room at position {room.Position}");

		RoomTemplate template = roomTemplates[random.Next(roomTemplates.Count)];
		room.Initialize(template); // Apply template logic

		PrintMap(room);
	}

	private Vector2 GetRandomPosition(Room room)
	{
		return new Vector2(random.Next(0, room.Width), random.Next(0, room.Height));
	}

	private void PrintMap(Room room)
	{
		GD.Print("Generated Room:");
		foreach (var primitive in room.Primitives)
		{
			GD.Print($"Placed {primitive.GetType().Name} at {primitive.GlobalPosition}");
		}
	}
}
