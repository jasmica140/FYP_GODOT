using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class KeyAtom : Atom {
	
	public KeyAtom() { }

	public KeyAtom(DoorColour colour) {
		// load texture based on key colour
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
				// invalid colour
				break;
		}
		
		// scale and size
		this.Scale = new Vector2(0.7f, 0.7f);
		Size = new Vector2(55, 35);
		
		// add collision
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
		// always valid
		return true;
	}
}

public partial class LockAtom : Atom {

	public LockAtom() { }
	
	public LockAtom(DoorColour colour) {
		// load texture based on lock colour
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
				// invalid colour
				break;
		}
		
		// scale and size
		this.Scale = new Vector2(0.5f, 0.5f);
		Size = new Vector2(70, 70);
		
		// add collision
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
		// always valid
		return true;
	}
}

public partial class DoorKey : Primitive {

	public DoorColour Colour { get; set; }

	public DoorKey() : base(Vector2.Zero) {
		Category = PrimitiveCategory.Collectible;
	}

	public DoorKey(Vector2 position) : base(position) {}
	
	public override bool GenerateInRoom(Room room) {
		// spawn key atom
		KeyAtom atom = new KeyAtom(Colour);
		atom.GlobalPosition = this.Position;
		AddAtom(atom);
		
		return room.AddPrimitive(this);
	}

	public override void GenerateAnchors(Room room) {
		Anchors.Clear();

		// place center anchor at key position
		Vector2 basePos = this.Position;
		float orbit = 40f;
		
		Anchors.Add(new Anchor(basePos, orbit, "center", this));
	}
}

public partial class DoorLock : Primitive {

	public DoorColour Colour { get; set; }

	public DoorLock() : base(Vector2.Zero) {
		Category = PrimitiveCategory.Environmental;
	}

	public DoorLock(Vector2 position) : base(position) {}
	
	public override bool GenerateInRoom(Room room) {
		// make sure there's a floor below the lock
		if (!room.HasAtomOfTypeAt(this.Position + new Vector2(0, 70), typeof(FloorTile))) {
			return false;
		}
		
		// spawn lock atom slightly above position
		LockAtom atom = new LockAtom(Colour);
		atom.GlobalPosition = this.Position - new Vector2(40, 70);
		AddAtom(atom);
		
		return room.AddPrimitive(this);
	}

	public override void GenerateAnchors(Room room) {
		// no anchors for lock
	}
}
