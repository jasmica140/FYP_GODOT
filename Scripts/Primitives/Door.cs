using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public enum DoorColour { Red, Blue, Green, Yellow }

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


public partial class OpenDoorBottomAtom : Atom {
	public OpenDoorBottomAtom() {
		SetTexture((Texture2D)GD.Load("res://Assets/kenney_platformer-art-deluxe/Base pack/Tiles/door_openMid.png")); 
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

public partial class OpenDoorTopAtom : Atom {
	public OpenDoorTopAtom() {
		SetTexture((Texture2D)GD.Load("res://Assets/kenney_platformer-art-deluxe/Base pack/Tiles/door_openTop.png")); 
		Size = new Vector2(35, 70);
	}
	
	public override bool ValidatePlacement(Room room) {
		// Ensure Mushroom is placed on a floor
		return true;
	}
}


public partial class Door : Primitive {
	
	public int LinkedRoomId = -1;
	public Vector2 LinkedDoorPosition;
	public DoorColour Colour;
	public bool isOpen = false;
	public bool isStartDoor = false;

	public Door() : base(Vector2.Zero) {	
		Category = PrimitiveCategory.Environmental;
	}  // Default constructor needed for instantiation
	
	public Door(Vector2 position) : base(position) { }
	
	public override bool GenerateInRoom(Room room) {
		DoorBottomAtom bottomAtom = new DoorBottomAtom();
		bottomAtom.GlobalPosition = this.Position;
		AddAtom(bottomAtom);
		
		DoorTopAtom topAtom = new DoorTopAtom();
		topAtom.GlobalPosition = this.Position - new Vector2(0, bottomAtom.Size.Y);
		AddAtom(topAtom);
		
		return room.AddPrimitive(this);
	}
	
	public override void GenerateAnchors(Room room) {
		Anchors.Clear();

		Atom tile = GetAtoms().First(); // Assume one atom

		Vector2 basePos = tile.GlobalPosition;
		float orbit = 40f;
		
		Anchors.Add(new Anchor(basePos, orbit, "center", this));
	}
	
	public void OpenDoor(Room room)
	{
		isOpen = true;
		
		// Remove existing door atoms from the room
		foreach (var atom in atoms)
		{
			room.Atoms.Remove(atom);     // Remove from room's atom list
			atom.QueueFree();            // Remove from scene tree
		}
		atoms.Clear(); // Clear the primitive's atom list

		// Create new open door atoms
		OpenDoorBottomAtom openBottom = new OpenDoorBottomAtom();
		openBottom.GlobalPosition = this.Position;

		OpenDoorTopAtom openTop = new OpenDoorTopAtom();
		openTop.GlobalPosition = this.Position - new Vector2(0, openBottom.Size.Y);

		// Add new atoms to primitive and to room
		AddAtom(openBottom);
		AddAtom(openTop);
		room.AddAtom(openBottom);
		room.AddAtom(openTop);

		//GD.Print($"🚪 Door at {this.Position} opened.");
	}
}
