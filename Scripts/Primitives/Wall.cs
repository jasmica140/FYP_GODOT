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
	}  // Required constructor

	public Wall(Vector2 position) : base(position) { }
	
	public override bool GenerateInRoom(Room room) {
		
		for (int x = 0; x < width; x++) {
			for (int y = 0; y < height; y++){
				FillerStoneTile tile = new FillerStoneTile();
				tile.GlobalPosition = this.Position + new Vector2(x * 70, y * 70);
				AddAtom(tile);
			}
		}
		return room.AddPrimitive(this);
	}
	
	public override void GenerateAnchors(Room room)
	{
		Anchors.Clear();
		InternalPaths.Clear();

		GenerateObstructionLines();
	}
	
	public void GenerateObstructionLines()
	{
		ObstructionLines.Clear();
		
		List<Atom> tiles = GetAtoms(); 
		Atom firstTile = tiles.First();
		Atom lastTile = tiles.Last();
		
		Vector2 topLeft = firstTile.GlobalPosition - firstTile.Size / 2;
		Vector2 bottomLeft = topLeft + new Vector2(0, height * 70);
		Vector2 bottomRight = lastTile.GlobalPosition + lastTile.Size / 2;
		Vector2 topRight = bottomRight - new Vector2(0, height * 70);

		//ObstructionLines.Add((topLeft, topRight));
		ObstructionLines.Add((topRight, bottomRight));
		ObstructionLines.Add((bottomRight, bottomLeft));
		ObstructionLines.Add((bottomLeft, topLeft));
	}
}
