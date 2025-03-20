using Godot;
using System;

public abstract partial class Atom : StaticBody2D
{
	protected Sprite2D sprite;

	public Atom()
	{
		sprite = new Sprite2D();
		AddChild(sprite);
	}

	public void SetTexture(Texture2D texture)
	{
		sprite.Texture = texture;
	}

	// Every Atom defines its own placement rules
	public abstract bool ValidatePlacement(Room room);
}
