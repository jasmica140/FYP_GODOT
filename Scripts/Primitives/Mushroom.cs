using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class MushroomAtom : Atom {
	public MushroomAtom() {
		SetTexture((Texture2D)GD.Load("res://Assets/kenney_platformer-art-deluxe/Mushroom expansion/PNG/tallShroom_red.png")); 
		Size = new Vector2(44, 41);
		
		CollisionShape2D collision = new CollisionShape2D();
		CapsuleShape2D shape = new CapsuleShape2D();
		shape.Radius = 6;
		shape.Height = 38; 
		collision.Shape = shape;
		collision.RotationDegrees = 90;
		collision.Position = new Vector2(-1, 1);

		AddChild(collision);
		AddToGroup("Mushroom");
		
		SetCollisionLayerValue(2, true);
		SetCollisionMaskValue(1, true);
	}
	
	public override bool ValidatePlacement(Room room) {
		// Ensure Mushroom is placed on a floor
		return true;
	}
}

public partial class Mushroom : Primitive
{
	public Mushroom() : base(Vector2.Zero) {	
		Category = PrimitiveCategory.Test;
	}  // Default constructor needed for instantiation
	
	public Mushroom(Vector2 position) : base(position) {}
	
	public override void GenerateInRoom(Room room) {
		List<Vector2> validPositions = room.GetPositionsAboveFloorTiles();

		if (validPositions.Count == 0) {
			GD.Print($"⚠️ WARNING: No valid floor tile positions found for {this.GetType().Name}");
			return;
		}

		// Pick a random valid position from the list
		Random random = new Random();
		Vector2 chosenPosition = validPositions[random.Next(validPositions.Count)];

		MushroomAtom atom = new MushroomAtom();
		atom.GlobalPosition = chosenPosition;
		AddAtom(atom);
		room.AddAtom(atom); 
		
		this.Position = chosenPosition;
		room.AddPrimitive(this);
	}
	
	public override void GenerateAnchors()
	{
		Anchors.Clear();

		Atom tile = GetAtoms().First(); // Assume one atom

		Vector2 basePos = tile.GlobalPosition;
		float orbit = 10f;

		// 🟢 Anchor at mushroom base
		Anchors.Add(new Anchor(basePos + new Vector2(0, tile.Size.Y / 2), orbit, "base"));

		// Estimate jump apex — e.g. 3 tiles up
		float jumpHeight = tile.Size.Y * 10;
		Vector2 apexPos = basePos + new Vector2(0, -jumpHeight);

		// 🔵 Apex anchor
		Anchors.Add(new Anchor(apexPos, orbit, "apex"));

		// 🔵 Apex side anchors
		Anchors.Add(new Anchor(apexPos + new Vector2(-tile.Size.X, 0), orbit, "left_apex"));
		Anchors.Add(new Anchor(apexPos + new Vector2(tile.Size.X, 0), orbit, "right_apex"));
	}
}
