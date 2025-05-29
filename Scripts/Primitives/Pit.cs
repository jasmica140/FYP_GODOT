using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Pit : Primitive
{
	public int Width;
	public int Depth;

	public Pit() : base(Vector2.Zero) {
		Category = PrimitiveCategory.Environmental;
	}

	public Pit(Vector2 position) : base(position) {}

	// tries to place pit inside room by removing floor and filler tiles
	public override bool GenerateInRoom(Room room) {
		Random random = new Random();

		// estimate max horizontal distance player can jump across
		float a = 0.5f * room.Player.gravity;
		float b = -room.Player.wallJump.airAcceleration;
		float c = 70f;

		float discriminant = b * b - 4f * a * c;
		float t = (-b + Mathf.Sqrt(discriminant)) / (2f * a);
		float maxHorizontal = room.Player.moveSpeed * t;

		int maxWidth = Mathf.CeilToInt(maxHorizontal * room.DifficultyPercent / 70f);
		int minWidth = 2;
		int minDepth = 2;
		int maxDepth = Mathf.CeilToInt(10 * room.DifficultyPercent);

		// loop through floor tiles in random order
		foreach (Primitive floorPrimitive in room.Primitives.Where(p => p is Floor).OrderBy(_ => random.Next())) {
			foreach (Atom floorAtom in floorPrimitive.GetAtoms()) {
				Vector2 basePos = floorAtom.GlobalPosition;
				int tileX = (int)(basePos.X / 70);
				int tileY = (int)(basePos.Y / 70);

				// check if there are enough tiles beneath for pit
				bool hasMinWidth = true;
				for (int dx = 0; dx < minWidth && hasMinWidth; dx++) {
					if (!room.HasAtomBelow(new Vector2((tileX + dx) * 70, tileY * 70), typeof(FillerStoneTile))) {
						hasMinWidth = false;
					}
				}
				if (!hasMinWidth) continue;

				// calculate max horizontal span available
				int availableWidth = 0;
				for (int dx = 0; dx <= maxWidth; dx++) {
					Vector2 check = new Vector2((tileX + dx) * 70, (tileY + 1) * 70);
					if (room.HasAtomOfTypeAt(check, typeof(FillerStoneTile)))
						availableWidth++;
					else
						break;
				}

				// determine final width
				maxWidth = Math.Min(maxWidth, availableWidth);
				Width = random.Next(Math.Min(minWidth, maxWidth), Math.Max(minWidth, maxWidth));
				if (Width < minWidth) continue;

				// calculate max vertical space below
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
				if (Depth < minDepth) continue;

				// ensure no tiles exist above pit area
				bool hasOverheadClearance = true;
				for (int dx = 0; dx < Width && hasOverheadClearance; dx++) {
					Vector2 above = new Vector2((tileX + dx) * 70, (tileY - 1) * 70);
					if (room.Atoms.Any(a => a.GlobalPosition == above)) {
						hasOverheadClearance = false;
					}
				}
				if (!hasOverheadClearance) continue;

				// ensure left, right, and bottom edges have walls
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
				if (!hasSideWalls || !hasBottomWall) continue;

				// delete tiles and anchors inside pit area
				for (int dx = 0; dx < Width; dx++) {
					for (int dy = 0; dy < Depth; dy++) {
						Vector2 pos = new Vector2((tileX + dx) * 70, (tileY + dy) * 70);
						Atom atom = room.Atoms.FirstOrDefault(a => (a is FloorTile || a is FillerStoneTile) && a.GlobalPosition == pos);
						if (atom != null) {
							Primitive owner = room.Primitives.FirstOrDefault(p => p.GetAtoms().Contains(atom));
							if (owner != null) {
								owner.RemoveAtom(atom);
								room.RemoveAnchorsAt(atom.GlobalPosition);
							}
							room.Atoms.Remove(atom);
						}
					}
				}

				// finalise pit properties
				this.Position = new Vector2(tileX * 70, tileY * 70);
				Difficulty = Mathf.RoundToInt(5 * ((Depth / (float)maxDepth) + (Width / (float)maxWidth)) / 2f);
				room.AddPrimitive(this);
				return true;
			}
		}

		return false;
	}

	// defines anchor points for pathfinding and obstruction lines for placement checks
	public override void GenerateAnchors(Room room)
	{
		Anchors.Clear();
		ObstructionLines.Clear();

		float tileSize = 70f;
		Vector2 topLeft = Position - new Vector2(0, tileSize / 2);

		// top anchors
		Vector2 topAnchorPos = topLeft + new Vector2(((Width - 1) * tileSize) / 2f, 0);
		Anchor topAnchor = new Anchor(topAnchorPos, 40f, "top", this);
		Anchor topLeftAnchor = new Anchor(topAnchorPos - new Vector2(Width * tileSize / 2, 0), 40f, "topLeft", this);
		Anchor topRightAnchor = new Anchor(topAnchorPos + new Vector2(Width * tileSize / 2, 0), 40f, "topRight", this);
		Anchors.Add(topAnchor);
		Anchors.Add(topLeftAnchor);
		Anchors.Add(topRightAnchor);

		InternalPaths.Add(new AnchorConnection(topLeftAnchor, topAnchor, false));
		InternalPaths.Add(new AnchorConnection(topRightAnchor, topAnchor, false));

		List<Anchor> leftAnchors = new();
		List<Anchor> rightAnchors = new();
		List<Anchor> bottomAnchors = new();

		// side anchors
		for (int dy = 0; dy < Depth / 2; dy++) {
			float y = topLeft.Y + dy * tileSize * 2;
			Vector2 leftPos = new Vector2(topLeft.X - tileSize / 2f, y);
			Vector2 rightPos = new Vector2(topLeft.X + Width * tileSize - tileSize / 2f, y);

			Anchor leftAnchor = new Anchor(leftPos, 40f, "left", this);
			Anchor rightAnchor = new Anchor(rightPos, 40f, "right", this);
			leftAnchors.Add(leftAnchor);
			rightAnchors.Add(rightAnchor);
			Anchors.Add(leftAnchor);
			Anchors.Add(rightAnchor);
		}

		// bottom anchors
		for (int dx = 0; dx < Width; dx++) {
			float x = topLeft.X + dx * tileSize;
			float y = topLeft.Y + Depth * tileSize - tileSize / 2f;
			Vector2 pos = new Vector2(x, y);
			Anchor anchor = new Anchor(pos, 40f, "bottom", this);
			bottomAnchors.Add(anchor);
			Anchors.Add(anchor);
		}

		// connect side anchors upward
		foreach (var right in rightAnchors) {
			foreach (var left in leftAnchors) {
				if (right.Position.Y < left.Position.Y) {
					InternalPaths.Add(new AnchorConnection(left, right, false));
					break;
				}
			}
		}
		foreach (var left in leftAnchors) {
			foreach (var right in rightAnchors) {
				if (left.Position.Y < right.Position.Y) {
					InternalPaths.Add(new AnchorConnection(right, left, false));
					break;
				}
			}
		}

		// connect bottom anchors to bottom of pit
		InternalPaths.Add(new AnchorConnection(bottomAnchors.First(), leftAnchors.Last(), false));
		InternalPaths.Add(new AnchorConnection(bottomAnchors.Last(), rightAnchors.Last(), false));
		for (int i = 0; i < bottomAnchors.Count - 1; i++) {
			var a = bottomAnchors[i];
			var b = bottomAnchors[i + 1];
			InternalPaths.Add(new AnchorConnection(a, b, true));
		}

		// connect top to center bottom
		if (bottomAnchors.Count > 0) {
			Anchor centerBottom = bottomAnchors[bottomAnchors.Count / 2];
			InternalPaths.Add(new AnchorConnection(topAnchor, centerBottom, false));
		}

		// add horizontal obstruction line across pit
		Vector2 leftEdge = topLeft + new Vector2(-tileSize / 2f, 0);
		Vector2 rightEdge = topLeft + new Vector2(Width * tileSize - tileSize / 2f, 0);
		ObstructionLines.Add((leftEdge, rightEdge));
	}
}
