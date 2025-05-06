using Godot;
using System;


public partial class FillerStoneTile : Atom {
	public FillerStoneTile() {
		SetTexture((Texture2D)GD.Load("res://Assets/kenney_platformer-art-deluxe/Base pack/Tiles/grassCenter.png")); 
		
		// Add a collision shape
		CollisionShape2D collision = new CollisionShape2D();
		RectangleShape2D shape = new RectangleShape2D();
		shape.Size = new Vector2(70, 70); 

		SetCollisionLayerValue(2, true);
		SetCollisionMaskValue(1, true);
		SetCollisionMaskValue(6, true);
		
		collision.Shape = shape;
		AddChild(collision);
	}
	
	public override bool ValidatePlacement(Room room) {
		return true;
	}
}
