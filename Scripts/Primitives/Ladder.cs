using Godot;
using System;
using System.Collections.Generic;

public partial class LadderTile : Atom {
	public LadderTile() {
		SetTexture((Texture2D)GD.Load("res://Assets/Sprites/Tiles/ladder_mid.png")); // Replace with actual path
		
		// Add a collision shape
		CollisionShape2D collision = new CollisionShape2D();
		RectangleShape2D shape = new RectangleShape2D();
		shape.Size = new Vector2(50, 70); 
		
		SetCollisionLayerValue(3, true);
		SetCollisionMaskValue(1, true);

		collision.Shape = shape;
		AddChild(collision);
		AddToGroup("Ladder");
	}
	
	public override bool ValidatePlacement(Room room) {
		return true;
	}
}

public partial class Ladder : Primitive {

	public Ladder() : base(Vector2.Zero) {
		Category = PrimitiveCategory.MovementModifier;
	}  // Required constructor

	public Ladder(Vector2 position) : base(position) {}

	private void addCollision() {
		
	}
	
	public override void GenerateInRoom(Room room) {
		List<Vector2> validPositions = room.GetPositionsAboveFloorTiles();

		if (validPositions.Count == 0) {
			GD.Print($"⚠️ WARNING: No valid floor tile positions found for {this.GetType().Name}");
			return;
		}
		
		// Pick a random valid position from the list
		Random random = new Random();
		Vector2 chosenPosition = validPositions[random.Next(validPositions.Count)];
		int numOfTiles = random.Next(2, 10);

		for (int y = 0; y < numOfTiles; y++) {
			Vector2 position =  chosenPosition - new Vector2(0, y * 70); 
			LadderTile tile = new LadderTile();
			tile.GlobalPosition = position;
			AddAtom(tile);
			room.AddAtom(tile); // ✅ `AddAtom()` is called here to place each FloorTile atom
		}
		
		this.Position = chosenPosition;
		room.AddPrimitive(this);
	}
	
	
}
