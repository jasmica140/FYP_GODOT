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
	public Vector2 position { get; set; }

	public Mushroom() : base(Vector2.Zero) {	
		Category = PrimitiveCategory.MovementModifier;
	}  // Default constructor needed for instantiation
	
	public Mushroom(Vector2 position) : base(position) {}
	
	public override bool GenerateInRoom(Room room) {
		MushroomAtom atom = new MushroomAtom();
		atom.GlobalPosition = position;
		AddAtom(atom);
		//room.AddAtom(atom); 
		
		this.Position = position;
		return room.AddPrimitive(this);
	}
	
	//public override void GenerateInRoom(Room room) {
		//List<Vector2> validPositions = room.GetPositionsAboveFloorTiles();
//
		//if (validPositions.Count == 0) {
			//GD.Print($"⚠️ WARNING: No valid floor tile positions found for {this.GetType().Name}");
			//return;
		//}
//
		//// Pick a random valid position from the list
		//Random random = new Random();
		//Vector2 chosenPosition = validPositions[random.Next(validPositions.Count)];
//
		//MushroomAtom atom = new MushroomAtom();
		//atom.GlobalPosition = chosenPosition;
		//AddAtom(atom);
		//room.AddAtom(atom); 
		//
		//this.Position = chosenPosition;
		//room.AddPrimitive(this);
	//}
	
	public override void GenerateAnchors(Room room)
	{
		Anchors.Clear();
		InternalPaths.Clear();

		Atom tile = GetAtoms().First(); // Mushroom base tile
		Vector2 basePos = tile.GlobalPosition;
		float orbit = 40f;

		// Add base anchor
		Anchor center = new Anchor(basePos, orbit, "mushroom_base");
		Anchors.Add(center);

		// Arc settings
		int arcSteps = 20;
		int arcSlices = 9; // Total arcs (e.g., 5 = far left, mid-left, center, mid-right, far right)
		float tileSize = 70f;
		float maxHorizontal = tileSize * 5;
		float verticalPeak = tileSize * 8;

		// Loop through arc slices (-1 to 1 range)
		for (int s = 0; s < arcSlices; s++)
		{
			float sideT = (s / (float)(arcSlices - 1)) * 2f - 1f; // Map [0,arcSlices-1] to [-1,1]

			List<Anchor> arcAnchors = new();
			Anchor prev = center;

			for (int i = 1; i <= arcSteps; i++)
			{
				float t = i / (float)arcSteps;

				float dx = sideT * maxHorizontal * t;
				float dy = -4 * verticalPeak * t * (1 - t); // parabolic curve

				Vector2 pos = basePos + new Vector2(dx, dy);
				Anchor arcAnchor = new Anchor(pos, orbit, "jump_arc");
				
				AnchorConnection anchorConnection = new AnchorConnection(prev, arcAnchor, false);
				if(!anchorConnection.IsConnectionObstructed(room)) {
					arcAnchors.Add(arcAnchor);
					Anchors.Add(arcAnchor);
					InternalPaths.Add(anchorConnection);
					prev = arcAnchor;
				}
			}
		}
	}
}
