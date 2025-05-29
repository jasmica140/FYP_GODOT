using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;


public partial class MapGenerator : Node
{
	private Random random = new Random();
	private List<RoomTemplate> roomTemplates = new List<RoomTemplate>();

	public override async void _Ready() 
	{
		LoadTemplates(); // setup template list
		GenerateMap();   // generate initial room
		//await monteCarloSim(); // for simulation
	}

	private async Task monteCarloSim()
	{
		// get needed nodes
		Node2D roomsContainer = GetNode<Node2D>("../RoomsContainer");
		Node2D primitivesContainer = GetNode<Node2D>("../PrimitivesContainer");
		Node2D playerSpawn = GetNode<Node2D>("../PlayerSpawn");
		Camera2D camera = GetTree().Root.FindChild("Camera2D", true, false) as Camera2D;

		int doors = 4;

		// output file path
		string filePath = $"/Users/jasminemicallef/Desktop/Uni Docs/Comp Sci/FYP/results/montecarlo_results_with_roomsizes_{doors}doors.csv";
		StringBuilder csv = new StringBuilder();
		csv.AppendLine("RoomWidth,RoomHeight,Difficulty,Score,AnchorsVisited,TotalAnchors,Goals,MaxGoals,AverageDifficulty,MaxDifficulty,VerticalModifiersUsed,MaxVerticalModifiers,PlayerAbilitiesUsed,MaxPlayerAbilities");

		// test room sizes
		List<Vector2I> roomSizes = new List<Vector2I> {
			new Vector2I(35, 30), new Vector2I(70, 65),
			new Vector2I(105, 100), new Vector2I(210, 50)
		};

		foreach (Vector2I roomSize in roomSizes) {

			int width = roomSize.X;
			int height = roomSize.Y;

			// set camera zoom + position manually for each room size
			float zoom = 0.45f;
			float roomCenterX = 1073.0f;
			float roomCenterY = 1083.0f;

			if (width == 70) {
				zoom = 0.27f;
				roomCenterX = 2138.0f;
				roomCenterY = 2304.0f;
			} else if (width == 105) {
				zoom = 0.19f;
				roomCenterX = 3427.0f;
				roomCenterY = 3599.0f;
			}

			camera.Zoom = new Vector2(zoom, zoom);
			camera.GlobalPosition = new Vector2(roomCenterX, roomCenterY);

			for (int difficulty = 5; difficulty <= 5; difficulty++) {
				for (int i = 0; i < 1; i++) {

					bool success = false;
					int attempt = 0;

					while (!success) {
						GD.Print($"⏱️ Attempt {attempt++} at Room Size {roomSize}, Difficulty {difficulty}, Iteration {i}");

						await ToSignal(GetTree(), "process_frame");

						primitivesContainer.Position = new Vector2(0, 0);

						ClearContainer(roomsContainer);
						ClearContainer(primitivesContainer);
						ClearContainer(playerSpawn);

						// create and add room
						Room room = new Room(width, height, difficulty);
						room.GlobalPosition = new Vector2(0, 0);
						roomsContainer.AddChild(room);

						// pick random template
						RoomTemplate template = roomTemplates[random.Next(roomTemplates.Count)];
						InterestingnessResult result = null;

						try {
							GD.Print("⚗️ Initializing room...");
							result = room.Initialize(template);
							room.QueueRedraw();
						} catch (Exception e) {
							GD.PrintErr($"💥 Exception during Initialize: {e.Message}");
						}

						if (result != null && !float.IsNaN(result.Score)) {
							// save result to csv
							csv.AppendLine($"{width},{height},{difficulty},{result.Score:F2},{result.AnchorsVisited},{result.TotalAnchors},{result.Goals},{result.MaxGoals},{result.AverageDifficulty:F2},{result.MaxDifficulty},{result.VerticalModifiersUsed},{result.MaxVerticalModifiers},{result.PlayerAbilitiesUsed},{result.MaxPlayerAbilities}");
							success = true;
							GD.Print("✅ Result saved");

							// take screenshot on final iteration
							if (i == 1) {
								await ToSignal(GetTree(), "process_frame");
								Image screenshot = GetViewport().GetTexture().GetImage();
								string ssFilePath = $"/Users/jasminemicallef/Desktop/Uni Docs/Comp Sci/FYP/results/analysis/screenshot_rs{roomSize}_d{difficulty}_{doors}doors_i{i}.png";
								screenshot.SavePng(ssFilePath);
								GD.Print($"📸 Screenshot saved to: {filePath}");
							}

						} else {
							GD.Print("🗑️ Skipping null room");
							room.QueueFree();
						}
					}
				}
			}
		}

		// write file (commented out for now)
		//File.WriteAllText(filePath, csv.ToString());
		GD.Print($"✅ Monte Carlo data saved to: {filePath}");
	}

	// deletes all children in given container
	async void ClearContainer(Node container) {
		foreach (Node child in container.GetChildren()) {
			child.GetParent()?.RemoveChild(child);
			child.QueueFree();
		}
	}

	// defines allowed templates
	private void LoadTemplates() {
		roomTemplates.Add(new RoomTemplate(RoomType.Challenge, 
			new List<Primitive.PrimitiveCategory> {
				Primitive.PrimitiveCategory.Floor,
				Primitive.PrimitiveCategory.Hazard,
				Primitive.PrimitiveCategory.Platform,
				Primitive.PrimitiveCategory.MovementModifier,
				Primitive.PrimitiveCategory.Test
			}
		));
	}

	// generates 1 default room at start
	private void GenerateMap() {
		Node2D roomsContainer = GetNode<Node2D>("../RoomsContainer");

		Room room = new Room(100, 40, 5);
		roomsContainer.AddChild(room);
		room.GlobalPosition = new Vector2(0, 0);

		RoomTemplate template = roomTemplates[random.Next(roomTemplates.Count)];
		room.Initialize(template);
	}

	// pick a random grid position in the room
	private Vector2 GetRandomPosition(Room room) {
		return new Vector2(random.Next(0, room.Width), random.Next(0, room.Height));
	}

	// print every primitive in room to console
	private void PrintMap(Room room) {
		GD.Print("Generated Room:");
		foreach (var primitive in room.Primitives) {
			GD.Print($"Placed {primitive.GetType().Name} at {primitive.Position}");
		}
	}
}
