using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class KeyAtom : Atom {
	
	public KeyAtom() { }

	public KeyAtom(DoorColour colour) {

		switch (colour) {
			case DoorColour.Red:
				SetTexture((Texture2D)GD.Load("res://Assets/kenney_platformer-art-deluxe/Base pack/Items/keyRed.png"));
				break;
			case DoorColour.Blue:
				SetTexture((Texture2D)GD.Load("res://Assets/kenney_platformer-art-deluxe/Base pack/Items/keyBlue.png"));
				break;
			case DoorColour.Green:
				SetTexture((Texture2D)GD.Load("res://Assets/kenney_platformer-art-deluxe/Base pack/Items/keyGreen.png"));
				break;
			case DoorColour.Yellow:
				SetTexture((Texture2D)GD.Load("res://Assets/kenney_platformer-art-deluxe/Base pack/Items/keyYellow.png"));
				break;
			default:
				GD.PrintErr("⚠️ Invalid key color.");
				break;
		}
		
		this.Scale = new Vector2(0.7f, 0.7f); // Scale down the key
		Size = new Vector2(55, 35);
		
		// Add a collision shape
		CollisionShape2D collision = new CollisionShape2D();
		RectangleShape2D shape = new RectangleShape2D();
		shape.Size = new Vector2(Size.X, Size.Y); 
		
		SetCollisionLayerValue(3, true);
		SetCollisionMaskValue(1, true);

		collision.Shape = shape;
		AddChild(collision);
		AddToGroup("Key");
	}
	
	public override bool ValidatePlacement(Room room) {
		// Ensure Mushroom is placed on a floor
		return true;
	}
}

public partial class LockAtom : Atom {

	public LockAtom() { }
	
	public LockAtom(DoorColour colour) {
		switch (colour) {
			case DoorColour.Red:
				SetTexture((Texture2D)GD.Load("res://Assets/kenney_platformer-art-deluxe/Base pack/Tiles/lock_red.png"));
				break;
			case DoorColour.Blue:
				SetTexture((Texture2D)GD.Load("res://Assets/kenney_platformer-art-deluxe/Base pack/Tiles/lock_blue.png"));
				break;
			case DoorColour.Green:
				SetTexture((Texture2D)GD.Load("res://Assets/kenney_platformer-art-deluxe/Base pack/Tiles/lock_green.png"));
				break;
			case DoorColour.Yellow:
				SetTexture((Texture2D)GD.Load("res://Assets/kenney_platformer-art-deluxe/Base pack/Tiles/lock_yellow.png"));
				break;
			default:
				GD.PrintErr("⚠️ Invalid key color.");
				break;
		}
		
		this.Scale = new Vector2(0.5f, 0.5f); // Scale down the lock
		Size = new Vector2(70, 70);
		
		// Add a collision shape
		CollisionShape2D collision = new CollisionShape2D();
		RectangleShape2D shape = new RectangleShape2D();
		shape.Size = new Vector2(Size.X, Size.Y); 
		
		SetCollisionLayerValue(3, true);
		SetCollisionMaskValue(1, true);

		collision.Shape = shape;
		AddChild(collision);
		AddToGroup("Lock");
	}
	
	public override bool ValidatePlacement(Room room) {
		// Ensure Mushroom is placed on a floor
		return true;
	}
}

public partial class DoorKey : Primitive {

	public DoorColour Colour { get; set; }

	public DoorKey() : base(Vector2.Zero) {	
		Category = PrimitiveCategory.Collectible;
	}  // Default constructor needed for instantiation
	
	public DoorKey(Vector2 position) : base(position) { }
	
	public override bool GenerateInRoom(Room room) {
		KeyAtom atom = new KeyAtom(Colour);
		atom.GlobalPosition = this.Position;
		AddAtom(atom);
		
		return room.AddPrimitive(this);
	}

	public override void GenerateAnchors(Room room) {
		Anchors.Clear();

		//Atom tile = GetAtoms().First(); // Assume one atom

		//Vector2 basePos = tile.GlobalPosition;
		Vector2 basePos = this.Position;
		float orbit = 40f;
		
		Anchors.Add(new Anchor(basePos, orbit, "center", this));
	}
}

public partial class DoorLock : Primitive {

	public DoorColour Colour { get; set; }

	public DoorLock() : base(Vector2.Zero) {	
		Category = PrimitiveCategory.Environmental;
	}  // Default constructor needed for instantiation
	
	public DoorLock(Vector2 position) : base(position) { }
	
	public override bool GenerateInRoom(Room room) {
		if (!room.HasAtomOfTypeAt(this.Position + new Vector2(0, 70), typeof(FloorTile))) { return false; }
		
		LockAtom atom = new LockAtom(Colour);
		atom.GlobalPosition = this.Position - new Vector2(40, 70);
		AddAtom(atom);
		
		return room.AddPrimitive(this);
	}

	public override void GenerateAnchors(Room room) {
		
	}
}
