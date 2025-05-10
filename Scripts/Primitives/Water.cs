using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class TopWaterTile : Atom {
	public TopWaterTile() {
		
		var animSprite = new AnimatedSprite2D();
		var frames = new SpriteFrames();
		
		frames.AddAnimation("waterWave");
		frames.SetAnimationSpeed("waterWave", 3); // 4 FPS
		frames.SetAnimationLoop("waterWave", true);
		
		frames.AddFrame("waterWave", GD.Load<Texture2D>("res://Assets/kenney_platformer-art-deluxe/Ice expansion/Tiles/iceWaterMidAlt.png"));
		//frames.AddFrame("waterWave", GD.Load<Texture2D>("res://Assets/kenney_platformer-art-deluxe/Ice expansion/Tiles/iceWaterMidAlt.png"));

		animSprite.SpriteFrames = frames;
		animSprite.Play("waterWave");

		AddChild(animSprite);
			
		// Add a collision shape
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
		
		var animSprite = new AnimatedSprite2D();
		var frames = new SpriteFrames();
		
		frames.AddAnimation("waterMove");
		frames.SetAnimationSpeed("waterMove", 3); // 4 FPS
		frames.SetAnimationLoop("waterMove", true);
		
		frames.AddFrame("waterMove", GD.Load<Texture2D>("res://Assets/kenney_platformer-art-deluxe/Ice expansion/Tiles/iceWaterDeepStars.png"));
		frames.AddFrame("waterMove", GD.Load<Texture2D>("res://Assets/kenney_platformer-art-deluxe/Ice expansion/Tiles/iceWaterDeepStarsAlt.png"));

		animSprite.SpriteFrames = frames;
		animSprite.Play("waterMove");

		AddChild(animSprite);
				
		// Add a collision shape
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
	public List<float> availableFishPositions = new();
	public float minFishSpacing = 350f; // 5 tiles at difficulty 1, 1 tile at difficulty 5
		
	public Water() : base(Vector2.Zero) {
		Category = PrimitiveCategory.Environmental;
	}  // Required constructor

	public Water(Vector2 position) : base(position) {}
	
	public void GenerateAvailableFishPositions() {
		availableFishPositions.Clear();
		
		if (Width < 5 && Depth > 5) {
			if (Width <= 3) {
				availableFishPositions.Add((float)Position.X - 35 + ((Width * 70) / 2));
			} else {
				availableFishPositions.Add((float)Position.X - 70 + ((Width * 70) / 2));
				availableFishPositions.Add((float)Position.X + 35 + ((Width * 70) / 2));
			}
		} else if (Width >= 5){
			float topY = Position.Y + 35;
			float bottomY = Position.Y + Depth * 70;
			float heightAvailable = bottomY - topY;

			int fishCount = Mathf.FloorToInt(heightAvailable / minFishSpacing);
			if (fishCount <= 0) return;

			// Step 1: Build evenly spaced positions (raw)
			List<float> rawPositions = new();
			for (int i = 0; i < fishCount; i++) {
				rawPositions.Add(topY + i * minFishSpacing);
			}

			// Step 2: Sort them into your desired order
			List<float> orderedPositions = new();

			// Always start with the top position
			if (rawPositions.Count > 0)
				orderedPositions.Add(rawPositions[0]);

			// Add middle position (closest to center)
			if (rawPositions.Count > 2) {
				int midIndex = rawPositions.Count / 2;
				if (!orderedPositions.Contains(rawPositions[midIndex]))
					orderedPositions.Add(rawPositions[midIndex]);
			}

			// Add bottom position (last one)
			if (rawPositions.Count > 1)
				orderedPositions.Add(rawPositions[^1]); // ^1 = last

			// Add remaining in-between positions
			for (int i = 0; i < rawPositions.Count; i++) {
				float y = rawPositions[i];
				if (!orderedPositions.Contains(y))
					orderedPositions.Add(y);
			}

			availableFishPositions = orderedPositions;
		}
		
	}

	public override bool GenerateInRoom(Room room) {
		Random random = new Random();

		int maxWidth = 20;
		int minWidth = 2;
		int minDepth = 2;
		int maxDepth = 20;

		foreach (Primitive floorPrimitive in room.Primitives.Where(p => p is Floor).OrderBy(_ => random.Next())) {
			foreach (Atom floorAtom in floorPrimitive.GetAtoms()) {
				Vector2 basePos = floorAtom.GlobalPosition;
				int tileX = (int)(basePos.X / 70);
				int tileY = (int)(basePos.Y / 70);

				bool hasMinWidth = true;
				for (int dx = 0; dx < minWidth && hasMinWidth; dx++) {
					if (!room.HasAtomBelow(new Vector2((tileX + dx) * 70, (tileY) * 70), typeof(FillerStoneTile))) {
						hasMinWidth = false;
					}
				}
				if (!hasMinWidth)
					continue;

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
				
				if (Width < minWidth) { continue; }

				int availableDepth = 0;
				for (int dy = 1; dy <= maxDepth; dy++) {
					bool allClear = true;
					for (int dx = 0; dx < Width; dx++) {
						Vector2 check = new Vector2((tileX + dx) * 70, (tileY + dy) * 70);
						if (!room.HasAtomOfTypeAt(check, typeof(FillerStoneTile))) { allClear = false; }
					}
					if (!allClear) break;
					availableDepth++;
				}
				maxDepth = Math.Min(maxDepth, availableDepth);
				Depth = random.Next(minDepth, maxDepth);
				
				if (Depth < minDepth) { continue; }

				bool hasOverheadClearance = true;
				for (int dx = 0; dx < Width && hasOverheadClearance; dx++) {
					Vector2 above = new Vector2((tileX + dx) * 70, (tileY - 1) * 70);
					if (room.Atoms.Any(a => a.GlobalPosition == above)) {
						hasOverheadClearance = false;
					}
				}
				if (!hasOverheadClearance)
					continue;

				bool hasSideWalls = true;
				for (int dy = 0; dy < Depth && hasSideWalls; dy++) {
					Vector2 left = new Vector2((tileX - 1) * 70, (tileY + dy) * 70);
					Vector2 right = new Vector2((tileX + Width) * 70, (tileY + dy) * 70);
					if (!room.HasAtomOfTypeAt(left, typeof(FillerStoneTile)) && !room.HasAtomOfTypeAt(left, typeof(FloorTile))) {
						hasSideWalls = false;
					}
					if (!room.HasAtomOfTypeAt(right, typeof(FillerStoneTile)) && !room.HasAtomOfTypeAt(right, typeof(FloorTile))) {
						hasSideWalls = false;
					}
				}

				bool hasBottomWall = true;
				for (int dx = 0; dx < Width; dx++) {
					Vector2 bottom = new Vector2((tileX + dx) * 70, (tileY + Depth) * 70);
					if (!room.HasAtomOfTypeAt(bottom, typeof(FillerStoneTile))) {
						hasBottomWall = false;
					}
				}

				if (!hasSideWalls || !hasBottomWall)
					continue;

				for (int dx = 0; dx < Width; dx++) {
					for (int dy = 0; dy < Depth; dy++) {
						Vector2 pos = new Vector2((tileX + dx) * 70, (tileY + dy) * 70);
						Atom atom = room.Atoms.FirstOrDefault(a => (a is FloorTile || a is FillerStoneTile) && a.GlobalPosition == pos);

						if (atom != null) {
							Primitive owner = room.Primitives.FirstOrDefault(p => p.GetAtoms().Contains(atom));
							if (owner != null) {
								owner.RemoveAtom(atom);
								room.RemoveAnchorsAt(atom.GlobalPosition); // remove anchors from affected area
							}
							room.Atoms.Remove(atom); // remove from global atom list
							if (atom is FloorTile) {
								TopWaterTile tile = new TopWaterTile(); // Create new water tile
								tile.GlobalPosition = atom.GlobalPosition;
								AddAtom(tile); 
							} else if (atom is FillerStoneTile) {
								FillerWaterTile tile = new FillerWaterTile(); // Create new water tile
								tile.GlobalPosition = atom.GlobalPosition;
								AddAtom(tile); 
							}
						}
					}
				}
				this.Position = new Vector2(tileX * 70, tileY * 70);
				
				if (room.AddPrimitive(this)) {
					minFishSpacing *= (1.2f - room.DifficultyPercent);
					GenerateAvailableFishPositions();
					return true;
				} else {
					return false;
				}
			}
		}

		GD.PrintErr("❌ No valid location found for pit.");
		return false;
	}
	
	public override void GenerateAnchors(Room room)
	{
		Anchors.Clear();

		List<Atom> tiles = GetAtoms(); // This should return the ladder tiles

		if (tiles.Count == 0)
			return;

		// Sort by Y to identify top and bottom
		tiles.Sort((a, b) => a.GlobalPosition.Y.CompareTo(b.GlobalPosition.Y));

		float orbit = 20f; // radius in pixels

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
