using Godot;
using System;
using System.Collections.Generic;

public partial class HeartContainer : Node2D
{
	private List<AnimatedSprite2D> hearts = new List<AnimatedSprite2D>();
	private int maxHealth = 10;
	
	public override void _Ready()
	{
		// Add each heart to the list
		for (int i = 0; i < 5; i++)
		{
			hearts.Add(GetNode<AnimatedSprite2D>($"HeartSprite{i + 1}"));
		}
	}

	public void UpdateHearts(int currentHealth)
	{
		for (int i = 0; i < hearts.Count; i++)
		{
			if (currentHealth >= (i + 1) * 2)
				hearts[i].Play("0");
			else if (currentHealth == (i * 2) + 1)
				hearts[i].Play("1");
			else
				hearts[i].Play("2");
		}
	}
}
