using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class MushroomAtom : Atom {
	public MushroomAtom() {
		// set texture + size
		SetTexture((Texture2D)GD.Load("res://Assets/kenney_platformer-art-deluxe/Mushroom expansion/PNG/tallShroom_red.png")); 
		Size = new Vector2(44, 41);
		
		// add capsule collision
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
		// always valid
		return true;
	}
}

public partial class Mushroom : Primitive {

	public Vector2 position { get; set; }

	public Mushroom() : base(Vector2.Zero) {
		Category = PrimitiveCategory.MovementModifier;
	}

	public Mushroom(Vector2 position) : base(position) {}

	public override bool GenerateInRoom(Room room) {
		// spawn mushroom atom
		MushroomAtom atom = new MushroomAtom();
		atom.GlobalPosition = position;
		AddAtom(atom);

		this.Position = position;
		return room.AddPrimitive(this);
	}
	
	public override void GenerateAnchors(Room room) {
		Anchors.Clear();
		InternalPaths.Clear();

		// base anchor on mushroom itself
		Atom tile = GetAtoms().First();
		Vector2 basePos = tile.GlobalPosition;
		float orbit = 40f;

		Anchor center = new Anchor(basePos, orbit, "mushroom_base", this);
		Anchors.Add(center);

		// arc settings for jump paths
		int arcSteps = 20;
		int arcSlices = 9;
		float tileSize = 70f;

		float verticalPeak = (room.Player.springJumpSpeed * room.Player.springJumpSpeed) / (2f * room.Player.gravity);
		float maxHorizontal = room.Player.moveSpeed * (2f * room.Player.springJumpSpeed / room.Player.gravity);

		// loop through arc slices from far left to far right
		for (int s = 0; s < arcSlices; s++) {
			float sideT = (s / (float)(arcSlices - 1)) * 2f - 1f;

			List<Anchor> arcAnchors = new();
			Anchor prev = center;

			for (int i = 1; i <= arcSteps; i++) {
				float t = i / (float)arcSteps;

				// compute parabolic curve
				float dx = sideT * maxHorizontal * t;
				float dy = -4 * verticalPeak * t * (1 - t);

				Vector2 pos = basePos + new Vector2(dx, dy);
				Anchor arcAnchor = new Anchor(pos, orbit, "jump_arc", this);

				AnchorConnection anchorConnection = new AnchorConnection(prev, arcAnchor, false);

				// only add arc if no obstacle in between
				if (!anchorConnection.IsConnectionObstructed(room)) {
					arcAnchors.Add(arcAnchor);
					Anchors.Add(arcAnchor);
					InternalPaths.Add(anchorConnection);
					prev = arcAnchor;
				}
			}
		}
	}
}
