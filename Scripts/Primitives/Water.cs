using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class TopWaterTile : Atom {
	public TopWaterTile() {
		
		// create animated sprite with one looping frame
		var animSprite = new AnimatedSprite2D();
		var frames = new SpriteFrames();
		
		frames.AddAnimation("waterWave");
		frames.SetAnimationSpeed("waterWave", 3);
		frames.SetAnimationLoop("waterWave", true);
		frames.AddFrame("waterWave", GD.Load<Texture2D>("res://Assets/kenney_platformer-art-deluxe/Ice expansion/Tiles/iceWaterMidAlt.png"));
		animSprite.SpriteFrames = frames;
		animSprite.Play("waterWave");

		AddChild(animSprite);
		
		// set up collision shape
		CollisionShape2D collision = new CollisionShape2D();
		RectangleShape2D shape = new RectangleShape2D();
		shape.Size = new Vector2(70, 45); 
		collision.Shape = shape;
		collision.Position = new Vector2(1, 11);
		
		SetCollisionLayerValue(5, true);
		SetCollisionMaskValue(1, true);
		
		AddChild(collision);
		AddToGroup("Water");
	}
	
	public override bool ValidatePlacement(Room room) {
		return true;
	}
}

public partial class FillerWaterTile : Atom {
	public FillerWaterTile() {
		
		// create animated sprite with two-frame loop
		var animSprite = new AnimatedSprite2D();
		var frames = new SpriteFrames();
		
		frames.AddAnimation("waterMove");
		frames.SetAnimationSpeed("waterMove", 3);
		frames.SetAnimationLoop("waterMove", true);
		frames.AddFrame("waterMove", GD.Load<Texture2D>("res://Assets/kenney_platformer-art-deluxe/Ice expansion/Tiles/iceWaterDeepStars.png"));
		frames.AddFrame("waterMove", GD.Load<Texture2D>("res://Assets/kenney_platformer-art-deluxe/Ice expansion/Tiles/iceWaterDeepStarsAlt.png"));
		animSprite.SpriteFrames = frames;
		animSprite.Play("waterMove");

		AddChild(animSprite);
		
		// add collision
		CollisionShape2D collision = new CollisionShape2D();
		RectangleShape2D shape = new RectangleShape2D();
		shape.Size = new Vector2(70, 70); 
		collision.Shape = shape;
		
		SetCollisionLayerValue(5, true);
		SetCollisionMaskValue(1, true);
		
		AddChild(collision);
		AddToGroup("Water");
	}
	
	public override bool ValidatePlacement(Room room) {
		return true;
	}
}

public partial class Water : Primitive {

	public int Width;
	public int Depth;

	// list of y-positions where fish can spawn
	public List<float> availableFishPositions = new();

	// minimum vertical spacing between fish (reduced as difficulty increases)
	public float minFishSpacing = 350f;

	// default constructor
	public Water() : base(Vector2.Zero) {
		Category = PrimitiveCategory.Environmental;
	}

	// position-aware constructor
	public Water(Vector2 position) : base(position) {}

	// compute valid y-positions for spawning fish based on water shape
	public void GenerateAvailableFishPositions() {
		availableFishPositions.Clear();

		// simpler logic for thin deep pools
		if (Width < 5 && Depth > 5) {
			if (Width <= 3) {
				availableFishPositions.Add((float)Position.X - 35 + ((Width * 70) / 2));
			} else {
				availableFishPositions.Add((float)Position.X - 70 + ((Width * 70) / 2));
				availableFishPositions.Add((float)Position.X + 35 + ((Width * 70) / 2));
			}
		}
		// detailed spacing logic for wider water
		else if (Width >= 5) {
			float topY = Position.Y + 35;
			float bottomY = Position.Y + Depth * 70;
			float heightAvailable = bottomY - topY;

			int fishCount = Mathf.FloorToInt(heightAvailable / minFishSpacing);
			if (fishCount <= 0) return;

			// raw list of evenly spaced y-positions
			List<float> rawPositions = new();
			for (int i = 0; i < fishCount; i++) {
				rawPositions.Add(topY + i * minFishSpacing);
			}

			// sort into more interesting priority
			List<float> orderedPositions = new();

			if (rawPositions.Count > 0)
				orderedPositions.Add(rawPositions[0]); // top

			if (rawPositions.Count > 2) {
				int midIndex = rawPositions.Count / 2;
				if (!orderedPositions.Contains(rawPositions[midIndex]))
					orderedPositions.Add(rawPositions[midIndex]); // middle
			}

			if (rawPositions.Count > 1)
				orderedPositions.Add(rawPositions[^1]); // bottom

			// add remaining values in order
			for (int i = 0; i < rawPositions.Count; i++) {
				float y = rawPositions[i];
				if (!orderedPositions.Contains(y))
					orderedPositions.Add(y);
			}

			availableFishPositions = orderedPositions;
		}
	}

	// attempts to generate a water primitive inside a room
	public override bool GenerateInRoom(Room room) {
		Random random = new Random();

		int maxWidth = 20;
		int minWidth = 2;
		int minDepth = 2;
		int maxDepth = 20;

		// loop through all floor tiles in random order
		foreach (Primitive floorPrimitive in room.Primitives.Where(p => p is Floor).OrderBy(_ => random.Next())) {
			foreach (Atom floorAtom in floorPrimitive.GetAtoms()) {
				Vector2 basePos = floorAtom.GlobalPosition;
				int tileX = (int)(basePos.X / 70);
				int tileY = (int)(basePos.Y / 70);

				// check for horizontal base support
				bool hasMinWidth = true;
				for (int dx = 0; dx < minWidth && hasMinWidth; dx++) {
					if (!room.HasAtomBelow(new Vector2((tileX + dx) * 70, (tileY) * 70), typeof(FillerStoneTile))) {
						hasMinWidth = false;
					}
				}
				if (!hasMinWidth)
					continue;

				// find max horizontal extent
				int availableWidth = 0;
				for (int dx = 0; dx <= maxWidth; dx++) {
					Vector2 check = new Vector2((tileX + dx) * 70, (tileY + 1) * 70);
					if (room.HasAtomOfTypeAt(check, typeof(FillerStoneTile)))
						availableWidth++;
					else
						break;
				}
				maxWidth = Math.Min(maxWidth, availableWidth);
				Width = random.Next(minWidth, maxWidth);
				if (Width < minWidth) continue;

				// find max vertical depth
				int availableDepth = 0;
				for (int dy = 1; dy <= maxDepth; dy++) {
					bool allClear = true;
					for (int dx = 0; dx < Width; dx++) {
						Vector2 check = new Vector2((tileX + dx) * 70, (tileY + dy) * 70);
						if (!room.HasAtomOfTypeAt(check, typeof(FillerStoneTile))) allClear = false;
					}
					if (!allClear) break;
					availableDepth++;
				}
				maxDepth = Math.Min(maxDepth, availableDepth);
				Depth = random.Next(minDepth, maxDepth);
				if (Depth < minDepth) continue;

				// check if tiles above are clear
				bool hasOverheadClearance = true;
				for (int dx = 0; dx < Width && hasOverheadClearance; dx++) {
					Vector2 above = new Vector2((tileX + dx) * 70, (tileY - 1) * 70);
					if (room.Atoms.Any(a => a.GlobalPosition == above)) {
						hasOverheadClearance = false;
					}
				}
				if (!hasOverheadClearance) continue;

				// check for walls on both sides
				bool hasSideWalls = true;
				for (int dy = 0; dy < Depth && hasSideWalls; dy++) {
					Vector2 left = new Vector2((tileX - 1) * 70, (tileY + dy) * 70);
					Vector2 right = new Vector2((tileX + Width) * 70, (tileY + dy) * 70);
					if (!room.HasAtomOfTypeAt(left, typeof(FillerStoneTile)) && !room.HasAtomOfTypeAt(left, typeof(FloorTile)))
						hasSideWalls = false;
					if (!room.HasAtomOfTypeAt(right, typeof(FillerStoneTile)) && !room.HasAtomOfTypeAt(right, typeof(FloorTile)))
						hasSideWalls = false;
				}

				// check bottom edge
				bool hasBottomWall = true;
				for (int dx = 0; dx < Width; dx++) {
					Vector2 bottom = new Vector2((tileX + dx) * 70, (tileY + Depth) * 70);
					if (!room.HasAtomOfTypeAt(bottom, typeof(FillerStoneTile)))
						hasBottomWall = false;
				}

				if (!hasSideWalls || !hasBottomWall)
					continue;

				// replace tiles with water
				for (int dx = 0; dx < Width; dx++) {
					for (int dy = 0; dy < Depth; dy++) {
						Vector2 pos = new Vector2((tileX + dx) * 70, (tileY + dy) * 70);
						Atom atom = room.Atoms.FirstOrDefault(a => (a is FloorTile || a is FillerStoneTile) && a.GlobalPosition == pos);

						if (atom != null) {
							Primitive owner = room.Primitives.FirstOrDefault(p => p.GetAtoms().Contains(atom));
							if (owner != null) {
								owner.RemoveAtom(atom);
								room.RemoveAnchorsAt(atom.GlobalPosition); // remove old anchors
							}
							room.Atoms.Remove(atom); // remove from global atom list

							// place water atom
							if (atom is FloorTile) {
								TopWaterTile tile = new TopWaterTile();
								tile.GlobalPosition = atom.GlobalPosition;
								AddAtom(tile);
							} else if (atom is FillerStoneTile) {
								FillerWaterTile tile = new FillerWaterTile();
								tile.GlobalPosition = atom.GlobalPosition;
								AddAtom(tile);
							}
						}
					}
				}

				this.Position = new Vector2(tileX * 70, tileY * 70);

				if (room.AddPrimitive(this)) {
					// fish spacing adjusts by difficulty
					minFishSpacing *= (1.2f - room.DifficultyPercent);
					GenerateAvailableFishPositions();
					return true;
				} else {
					return false;
				}
			}
		}
		return false;
	}

	// generate anchors for fish and pathfinding
	public override void GenerateAnchors(Room room) {
		Anchors.Clear();

		List<Atom> tiles = GetAtoms();
		if (tiles.Count == 0)
			return;

		tiles.Sort((a, b) => a.GlobalPosition.Y.CompareTo(b.GlobalPosition.Y));

		float orbit = 20f;

		Vector2 offsetUp = new Vector2(0, -tiles.First().Size.Y / 2);
		Vector2 offsetDown = new Vector2(0, tiles.First().Size.Y / 2);
		Vector2 offsetSide = new Vector2(tiles.First().Size.X / 2, 0);

		foreach (Atom tile in tiles) {
			Vector2 pos = tile.GlobalPosition;

			Anchor topLeft = new Anchor(pos + offsetUp - offsetSide, orbit, "topLeft", this);
			Anchor bottomLeft = new Anchor(pos + offsetDown - offsetSide, orbit, "bottomLeft", this);
			Anchor topRight = new Anchor(pos + offsetUp + offsetSide, orbit, "topRight", this);
			Anchor bottomRight = new Anchor(pos + offsetDown + offsetSide, orbit, "bottomRight", this);

			Anchors.Add(topLeft);
			Anchors.Add(bottomLeft);
			Anchors.Add(topRight);
			Anchors.Add(bottomRight);

			InternalPaths.Add(new AnchorConnection(topLeft, topRight));
			InternalPaths.Add(new AnchorConnection(topLeft, bottomLeft));
			InternalPaths.Add(new AnchorConnection(bottomRight, bottomLeft));
		}
	}
}
