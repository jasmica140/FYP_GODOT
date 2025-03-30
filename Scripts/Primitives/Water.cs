using Godot;
using System;
using System.Collections.Generic;

public partial class WaterTile : Atom {
	public WaterTile() {
		SetTexture((Texture2D)GD.Load("res://Assets/kenney_platformer-art-deluxe/Base pack/Tiles/liquidWaterTop_mid.png")); 
		
		// Add a collision shape
		CollisionShape2D collision = new CollisionShape2D();
		RectangleShape2D shape = new RectangleShape2D();
		shape.Size = new Vector2(70, 70); 

		SetCollisionLayerValue(2, true);
		SetCollisionMaskValue(1, true);
		
		collision.Shape = shape;
		AddChild(collision);
		AddToGroup("Water");
	}
	
	public override bool ValidatePlacement(Room room) {
		return true;
	}
}

public partial class Water : Primitive {
	public List<WaterTile> waterTiles = new List<WaterTile>();

	public Water() : base(Vector2.Zero) {
		Category = PrimitiveCategory.Test;
	}  // Required constructor

	public Water(Vector2 position) : base(position) {}

	public override void GenerateInRoom(Room room) {
		// Look for the Floor primitive in the room
		Primitive floorPrimitive = room.Primitives.Find(p => p is Floor);

		//if (floorPrimitive != null) {
			//List <Atom> floorAtoms = floorPrimitive.GetAtoms();
			//GD.Print(floorAtoms.Count);
			//foreach (Atom atom in floorPrimitive.GetAtoms()) {
				//if (atom is FloorTile) {
					//WaterTile tile = new WaterTile(); // Create new water tile
					//tile.GlobalPosition = atom.GlobalPosition;
					//waterTiles.Add(tile);
					//AddChild(tile); 
					//room.AddAtom(tile);
					//floorPrimitive.ReplaceAtom(atom, tile); // Replace floor tile
				//} 
			//}
			//room.AddPrimitive(this);
			//GD.Print("💧 Floor tiles replaced with water tiles.");
		//} else {
			//GD.PrintErr("❌ No Floor primitive found in the room!");
		//}
	}
	
	//public override void GenerateInRoom(Room room) {
		//List<Atom> tilesToReplace = new List<Atom>();
		//List<Atom> Atoms = getAtoms();
		 //
		//foreach (var atom in Atoms) {
			//if (atom is FloorTile) { // Or use a group or tag if preferred
				//Vector2 pos = atom.GlobalPosition;
				//if (pos.X > 1400 && pos.Y == 1750) {
					//tilesToReplace.Add(atom);
				//}
			//}
		//}
		//
		//foreach (var tile in tilesToReplace) {
			//Atoms.Remove(tile);         // Remove from internal tracking list
			//tile.QueueFree();           // Remove from the scene
		//}
		//
		//foreach (var tile in tilesToReplace) {
			//WaterTile tile = new WaterTile();
			//tile.GlobalPosition = tile.GlobalPosition;
			//waterTiles.Add(tile);
			//AddChild(tile); 
			//room.AddAtom(tile);
		//}
		//
		//room.AddPrimitive(this);
	//}
}
