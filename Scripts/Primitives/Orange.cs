using Godot;
using System;

public partial class OrangeAtom : Atom {
	
	public OrangeAtom() {

		SetTexture((Texture2D)GD.Load("res://Assets/kenney_platformer-art-deluxe/Candy expansion/Tiles/cherry.png"));

		
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
		AddToGroup("Orange");
	}
	
	public override bool ValidatePlacement(Room room) {
		return true;
	}
}

public partial class Orange : Primitive {

	public Orange() : base(Vector2.Zero) {	
		Category = PrimitiveCategory.Collectible;
	}  // Default constructor needed for instantiation
	
	public Orange(Vector2 position) : base(position) { }
	
	public override bool GenerateInRoom(Room room) {
		OrangeAtom atom = new OrangeAtom();
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
