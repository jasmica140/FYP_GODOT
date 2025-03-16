using System.Collections.Generic;

public enum RoomType
{
	Puzzle,
	Challenge,
	Boss,
	PowerUp,
	Save
}

public class RoomTemplate
{
	public RoomType Type { get; private set; }
	public List<Primitive.PrimitiveCategory> RequiredPrimitiveCategories { get; private set; }

	public RoomTemplate(RoomType type, List<Primitive.PrimitiveCategory> requiredCategories) {
		Type = type;
		RequiredPrimitiveCategories = requiredCategories;
	}
}
