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
	}  // Default constructor needed for instantiation
	
	public Pit(Vector2 position) : base(position) {}
	
	public override bool GenerateInRoom(Room room) {
		Random random = new Random();

		float a = 0.5f * room.Player.gravity;
		float b = -room.Player.wallJump.airAcceleration;
		float c = 70f;

		float discriminant = b * b - 4f * a * c;
		float t = (-b + Mathf.Sqrt(discriminant)) / (2f * a);
		float maxHorizontal = room.Player.moveSpeed * t;
		int maxWidth = Mathf.CeilToInt(maxHorizontal * room.DifficultyPercent / 70f);
		
		int minWidth = 2;
		int minDepth = 2;
		int maxDepth =  Mathf.CeilToInt(10 * room.DifficultyPercent);

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
						}
					}
				}

				this.Position = new Vector2(tileX * 70, tileY * 70);
				room.AddPrimitive(this);
				GD.Print($"✅ Pit placed at ({tileX}, {tileY}) with size {Width}x{Depth}");
				return true;
			}
		}

		GD.PrintErr("❌ No valid location found for pit.");
		return false;
	}

	public override void GenerateAnchors(Room room)
	{
		Anchors.Clear();
		ObstructionLines.Clear();

		float tileSize = 70f;
		Vector2 topLeft = Position;

		// === TOP ANCHOR ===
		Vector2 topAnchorPos = topLeft + new Vector2((Width * tileSize) / 2f, -tileSize / 2f);
		Anchor topAnchor = new Anchor(topAnchorPos, 40f, "top");
		Anchors.Add(topAnchor);

		List<Anchor> leftAnchors = new();
		List<Anchor> rightAnchors = new();
		List<Anchor> bottomAnchors = new();

		// === SIDE ANCHORS ===
		for (int dy = 0; dy < Depth; dy++)
		{
			float y = topLeft.Y + dy * tileSize;

			Vector2 leftPos = new Vector2(topLeft.X - tileSize / 2f, y);
			Vector2 rightPos = new Vector2(topLeft.X + Width * tileSize - tileSize / 2f, y); // shifted left

			Anchor leftAnchor = new Anchor(leftPos, 40f, "left");
			Anchor rightAnchor = new Anchor(rightPos, 40f, "right");

			leftAnchors.Add(leftAnchor);
			rightAnchors.Add(rightAnchor);
			Anchors.Add(leftAnchor);
			Anchors.Add(rightAnchor);
		}

		// === BOTTOM ANCHORS ===
		for (int dx = 0; dx < Width; dx++)
		{
			float x = topLeft.X + dx * tileSize;
			float y = topLeft.Y + Depth * tileSize - tileSize / 2f;

			Vector2 pos = new Vector2(x, y);
			Anchor anchor = new Anchor(pos, 40f, "bottom");
			bottomAnchors.Add(anchor);
			Anchors.Add(anchor);
		}

		// === INTERNAL CONNECTIONS ===

		// ⬆️ Left → Right (only upward)
		foreach (var left in leftAnchors)
		{
			foreach (var right in rightAnchors)
			{
				if (right.Position.Y < left.Position.Y)
				{
					InternalPaths.Add(new AnchorConnection(left, right, false));
				}
			}
		}

		// ⬆️ Bottom → first 3 left/right
		foreach (var bottom in bottomAnchors)
		{
			foreach (var left in leftAnchors.Take(3))
				InternalPaths.Add(new AnchorConnection(bottom, left, false));
			foreach (var right in rightAnchors.Take(3))
				InternalPaths.Add(new AnchorConnection(bottom, right, false));
		}

		// ↔️ Bottom ↔ Bottom (bidirectional)
		for (int i = 0; i < bottomAnchors.Count - 1; i++)
		{
			var a = bottomAnchors[i];
			var b = bottomAnchors[i + 1];
			InternalPaths.Add(new AnchorConnection(a, b, true));
		}

		// ⬇️ Top → center bottom
		if (bottomAnchors.Count > 0)
		{
			Anchor centerBottom = bottomAnchors[bottomAnchors.Count / 2];
			InternalPaths.Add(new AnchorConnection(topAnchor, centerBottom, false));
		}

		// === OBSTRUCTION LINE (centered horizontally at top of pit) ===
		Vector2 leftEdge = topLeft + new Vector2(-tileSize / 2f, -tileSize / 2f);
		Vector2 rightEdge = topLeft + new Vector2(Width * tileSize - tileSize / 2f, -tileSize / 2f);
		ObstructionLines.Add((leftEdge, rightEdge));
	}
}
