using System;
using System.Collections.Generic;

public static class CompatibilityMatrix
{
	private static Dictionary<Type, Dictionary<Type, float>> matrix = new();

	public static void SetCompatibility(Type from, Type to, float probability)
	{
		if (!matrix.ContainsKey(from))
		{
			matrix[from] = new Dictionary<Type, float>();
		}
		matrix[from][to] = probability;
	}

	public static float GetCompatibility(Type from, Type to)
	{
		if (matrix.ContainsKey(from) && matrix[from].ContainsKey(to))
			return matrix[from][to];

		return 0f; // Default to 0 if no match is found
	}

	public static Dictionary<Type, float> GetCompatibleTypes(Type from)
	{
		if (matrix.ContainsKey(from))
			return matrix[from];

		return new Dictionary<Type, float>();
	}
	
	public static Type WeightedRandomChoice(Dictionary<Type, float> options)
	{
		float totalWeight = 0f;
		foreach (float weight in options.Values)
			totalWeight += weight;

		Random rng = new Random();
		float randomPoint = (float)(rng.NextDouble() * totalWeight);

		foreach (var kvp in options)
		{
			if (randomPoint < kvp.Value)
				return kvp.Key;
			randomPoint -= kvp.Value;
		}

		return null;
	}
	
	public static void Initialize()
	{
		// FLOOR
		SetCompatibility(typeof(Floor), typeof(Ladder), 0.9f);
		SetCompatibility(typeof(Floor), typeof(Mushroom), 0.7f);
		//SetCompatibility(typeof(Floor), typeof(Spike), 0.6f);
		//SetCompatibility(typeof(Floor), typeof(Anvil), 0.4f);
		SetCompatibility(typeof(Floor), typeof(Cactus), 0.3f);
		//SetCompatibility(typeof(Floor), typeof(Pit), 0.3f);
		SetCompatibility(typeof(Floor), typeof(Water), 0.3f);

		// LADDER
		SetCompatibility(typeof(Ladder), typeof(Floor), 0.9f);
		SetCompatibility(typeof(Ladder), typeof(Platform), 0.8f);
		SetCompatibility(typeof(Ladder), typeof(StickyFloor), 0.3f);

		// MUSHROOM
		SetCompatibility(typeof(Mushroom), typeof(Floor), 0.7f);
		SetCompatibility(typeof(Mushroom), typeof(Platform), 0.6f);
		//SetCompatibility(typeof(Mushroom), typeof(Spike), 0.2f);

		// PLATFORM
		SetCompatibility(typeof(Platform), typeof(Ladder), 0.8f);
		SetCompatibility(typeof(Platform), typeof(Mushroom), 0.6f);
		//SetCompatibility(typeof(Platform), typeof(Spike), 0.3f);
		//SetCompatibility(typeof(Platform), typeof(Key), 0.4f);

		// SPIKE
		//SetCompatibility(typeof(Spike), typeof(Floor), 0.6f);
		//SetCompatibility(typeof(Spike), typeof(Platform), 0.3f);
		//SetCompatibility(typeof(Spike), typeof(Mushroom), 0.2f);
//
		//// PIT
		//SetCompatibility(typeof(Pit), typeof(Platform), 0.5f);
		//SetCompatibility(typeof(Pit), typeof(Spike), 0.5f);
//
		//// KEY / DIAMOND / COLLECTIBLES
		//SetCompatibility(typeof(Key), typeof(Platform), 0.5f);
		//SetCompatibility(typeof(Key), typeof(Ladder), 0.4f);
		//SetCompatibility(typeof(Diamond), typeof(Platform), 0.5f);
		//SetCompatibility(typeof(Diamond), typeof(Floor), 0.3f);
//
		//// SWITCH / BUTTON
		//SetCompatibility(typeof(Switch), typeof(Platform), 0.5f);
		//SetCompatibility(typeof(Switch), typeof(Floor), 0.5f);
//
		//// BOMB
		//SetCompatibility(typeof(Bomb), typeof(Anvil), 0.3f);
		//SetCompatibility(typeof(Bomb), typeof(Cactus), 0.2f);

		// WATER / LAVA
		SetCompatibility(typeof(Water), typeof(Platform), 0.7f);
		SetCompatibility(typeof(Water), typeof(Floor), 0.3f);
		//SetCompatibility(typeof(Lava), typeof(Platform), 0.7f);
		//SetCompatibility(typeof(Lava), typeof(Floor), 0.2f);
	}
}
