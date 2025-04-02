using Godot;
using System;

public abstract partial class Atom : StaticBody2D
{
	protected Sprite2D sprite;
	public Vector2 Size { get; protected set; } = new Vector2(70, 70); // default size (override per atom)

	public Atom()
	{
		sprite = new Sprite2D();
		AddChild(sprite);
	}

	public void SetTexture(Texture2D texture)
	{
		sprite.Texture = texture;
	}

	// every atom defines its own placement rules
	public abstract bool ValidatePlacement(Room room);
}
