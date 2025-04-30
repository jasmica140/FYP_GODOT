//using Godot;
//using System.Collections.Generic;
//using System.Collections.Generic;
//using System.Linq;
//
//public static class RoomManager
//{
	//private static List<Room> visitedRooms = new List<Room>(); // Keeps track of all visited/generated rooms
	//private static Room currentRoom = null; // Keeps track of the active room
//
	//public static void Initialize(Room startingRoom)
	//{
		//currentRoom = startingRoom;
		//visitedRooms.Add(startingRoom);
	//}
//
	//public static void GoToRoomFromDoor(Node context, Door door, PlayerController player)
	//{
		//GD.Print("🚪 Player entered door: generating or retrieving room!");
//
		//Room nextRoom = GenerateNewRoom(context);
//
		//if (nextRoom != null)
		//{
			//// Remove the current room
			//if (currentRoom != null)
				//currentRoom.QueueFree();
//
			//currentRoom = nextRoom;
//
			//// Find RoomsContainer if you have one
			//Node main = context.GetTree().CurrentScene; // This is your Main scene
			//Node2D roomsContainer = main.FindNode("RoomsContainer", true, false) as Node2D;
			//
			//if (roomsContainer != null)
			//{
				//roomsContainer.AddChild(nextRoom);
			//}
			//else
			//{
				//context.GetTree().Root.AddChild(nextRoom); // Fallback
			//}
//
			//// Find PlayerSpawn point
			//Node2D playerSpawn = main.FindNode("PlayerSpawn", true, false) as Node2D;
			//if (playerSpawn != null)
			//{
				//player.GlobalPosition = playerSpawn.GlobalPosition;
				//playerSpawn.AddChild(player); // Add to spawn
			//}
			//else
			//{
				//// Fallback if no spawn point found
				//nextRoom.AddChild(player);
				//player.GlobalPosition = new Vector2(300, 300); // Or any safe default
			//}
		//}
	//}
//
	//private static Room GenerateNewRoom(Node context)
	//{
		//GD.Print("🛠️ Generating new room...");
//
		//Room newRoom = new Room(40, 26); // Set whatever width/height you want
		//context.GetTree().Root.AddChild(newRoom);
//
		//visitedRooms.Add(newRoom);
//
		//return newRoom;
	//}
//
	//public static Room GetCurrentRoom()
	//{
		//return currentRoom;
	//}
//}
