using Tripper.Tools.Math;

namespace Styx.CommonBot.AreaManagement
{
    public class PvPArea : PolygonArea
    {
        public readonly string Name;

        public PvPArea(string name, params Vector2[] areaDefinition)
            : base(areaDefinition)
        {
            Name = name;
        }

        public PvPArea(params Vector2[] areaDefinition)
            : this("Unknown", areaDefinition)
        {
        }

        public override AreaType Type
        {
            get { return AreaType.PvP; }
        }
    }
}
