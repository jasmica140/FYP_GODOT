using Godot;
using System;
using System.Collections.Generic;

public partial class HeartContainer : Node2D
{
	private List<AnimatedSprite2D> hearts = new List<AnimatedSprite2D>();
	private int maxHealth = 10;
	
	public override void _Ready()
	{
		// grab references to heart sprites
		for (int i = 0; i < 5; i++)
		{
			hearts.Add(GetNode<AnimatedSprite2D>($"HeartSprite{i + 1}"));
		}
	}

	public void UpdateHearts(int currentHealth)
	{
		for (int i = 0; i < hearts.Count; i++)
		{
			// full heart if player has 2 hp for this slot
			if (currentHealth >= (i + 1) * 2)
				hearts[i].Play("0");

			// half heart if player has exactly 1 hp for this slot
			else if (currentHealth == (i * 2) + 1)
				hearts[i].Play("1");

			// empty heart otherwise
			else
				hearts[i].Play("2");
		}
	}
}
