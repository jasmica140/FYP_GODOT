using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Wall : Primitive {

	public Zone zone { get; set; }
	public int width { get; set; }
	public int height { get; set; }

	public Wall() : base(Vector2.Zero) {
		Category = PrimitiveCategory.Environmental;
	}  // required constructor for instantiation

	public Wall(Vector2 position) : base(position) { }

	// place filler tiles in room based on width × height
	public override bool GenerateInRoom(Room room) {
		for (int x = 0; x < width; x++) {
			for (int y = 0; y < height; y++){
				FillerStoneTile tile = new FillerStoneTile();
				tile.GlobalPosition = this.Position + new Vector2(x * 70, y * 70);
				AddAtom(tile); // add each tile to primitive
			}
		}
		return room.AddPrimitive(this);
	}

	// generate wall obstruction lines (no anchors used)
	public override void GenerateAnchors(Room room) {
		Anchors.Clear();
		InternalPaths.Clear();
		GenerateObstructionLines();
	}

	// add obstruction lines to block pathfinding
	public void GenerateObstructionLines() {
		ObstructionLines.Clear();
		
		List<Atom> tiles = GetAtoms(); 
		Atom firstTile = tiles.First();
		Atom lastTile = tiles.Last();
		
		// calculate corners of wall rectangle
		Vector2 topLeft = firstTile.GlobalPosition - firstTile.Size / 2;
		Vector2 bottomLeft = topLeft + new Vector2(0, height * 70);
		Vector2 bottomRight = lastTile.GlobalPosition + lastTile.Size / 2;
		Vector2 topRight = bottomRight - new Vector2(0, height * 70);

		// draw U-shape: right, bottom, left (top skipped)
		//ObstructionLines.Add((topLeft, topRight)); // intentionally skipped
		ObstructionLines.Add((topRight, bottomRight));
		ObstructionLines.Add((bottomRight, bottomLeft));
		ObstructionLines.Add((bottomLeft, topLeft));
	}
}
