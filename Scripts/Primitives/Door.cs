using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class DoorBottomAtom : Atom {
	public DoorBottomAtom() {
		SetTexture((Texture2D)GD.Load("res://Assets/kenney_platformer-art-deluxe/Buildings expansion/Tiles/doorKnobAlt.png")); 
		Size = new Vector2(70, 70);
		
		// Add a collision shape
		CollisionShape2D collision = new CollisionShape2D();
		RectangleShape2D shape = new RectangleShape2D();
		shape.Size = new Vector2(Size.X-20, Size.Y); 
		
		SetCollisionLayerValue(3, true);
		SetCollisionMaskValue(1, true);

		collision.Shape = shape;
		AddChild(collision);
		AddToGroup("Door");
	}
	
	public override bool ValidatePlacement(Room room) {
		// Ensure Mushroom is placed on a floor
		return true;
	}
}

public partial class DoorTopAtom : Atom {
	public DoorTopAtom() {
		SetTexture((Texture2D)GD.Load("res://Assets/kenney_platformer-art-deluxe/Buildings expansion/Tiles/doorPlateTop.png")); 
		Size = new Vector2(35, 70);
	}
	
	public override bool ValidatePlacement(Room room) {
		// Ensure Mushroom is placed on a floor
		return true;
	}
}

public partial class Door : Primitive {
	
	public Door() : base(Vector2.Zero) {	
		Category = PrimitiveCategory.Test;
	}  // Default constructor needed for instantiation
	
	public Door(Vector2 position) : base(position) { }
	
	public override void GenerateInRoom(Room room) {
		List<Vector2> validPositions = room.GetPositionsAboveFloorTiles();

		if (validPositions.Count == 0) {
			GD.Print($"⚠️ WARNING: No valid floor tile positions found for {this.GetType().Name}");
			return;
		}

		// Pick a random valid position from the list
		Random random = new Random();
		Vector2 chosenPosition = validPositions[random.Next(validPositions.Count)];

		DoorBottomAtom bottomAtom = new DoorBottomAtom();
		bottomAtom.GlobalPosition = chosenPosition;
		AddAtom(bottomAtom);
		room.AddAtom(bottomAtom); 
		
		DoorTopAtom topAtom = new DoorTopAtom();
		topAtom.GlobalPosition = chosenPosition - new Vector2(0,bottomAtom.Size.Y);
		AddAtom(topAtom);
		room.AddAtom(topAtom); 
		
		this.Position = chosenPosition;
		room.AddPrimitive(this);
	}
	
	public override void GenerateAnchors() {
		Anchors.Clear();

		Atom tile = GetAtoms().First(); // Assume one atom

		Vector2 basePos = tile.GlobalPosition;
		float orbit = 10f;
		
		Anchors.Add(new Anchor(basePos, orbit, "centre"));
	}
}
