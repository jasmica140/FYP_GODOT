using Godot;
using System;

public abstract partial class Atom : Node2D
{
	public Sprite2D sprite;

	public Atom()
	{
		sprite = new Sprite2D();
		AddChild(sprite);
	}

	public void SetTexture(Texture2D texture)
	{
		sprite.Texture = texture;
	}
}
