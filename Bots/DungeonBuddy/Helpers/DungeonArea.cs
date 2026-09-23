using Styx.CommonBot.AreaManagement;
using Tripper.Tools.Math;

namespace Bots.DungeonBuddy.Helpers;

public class DungeonArea : PolygonArea
{
	public override AreaType Type => AreaType.Polygon;

	public DungeonArea(params Vector2[] areaDefinition)
		: base(areaDefinition)
	{
	}
}
