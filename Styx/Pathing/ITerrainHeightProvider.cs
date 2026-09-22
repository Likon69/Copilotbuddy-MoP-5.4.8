using System.Collections.Generic;

namespace Styx.Pathing
{
    public interface ITerrainHeightProvider
    {
        List<float> FindHeights(float x, float y);
    }
}
