using System.Collections.Generic;
using Tripper.Tools.Math;

namespace Styx.CommonBot.AreaManagement
{
    public abstract class PolygonArea : Area
    {
        protected PolygonArea(params Vector2[] areaDefinition)
        {
            VertX = new List<float>();
            VertY = new List<float>();
            foreach (Vector2 vector in areaDefinition)
            {
                VertX.Add(vector.X);
                VertY.Add(vector.Y);
            }
        }

        private List<float> VertX { get; set; }
        private List<float> VertY { get; set; }

        public bool LocalPlayerIsInBounds
        {
            get { return PnPoly(VertX.Count, VertX.ToArray(), VertY.ToArray(), StyxWoW.Me.X, StyxWoW.Me.Y); }
        }

        public bool IsPointInPoly(WoWPoint point)
        {
            return PnPoly(VertX.Count, VertX.ToArray(), VertY.ToArray(), point.X, point.Y);
        }

        /// <summary>
        /// Tests whether a point is inside a polygon.
        /// </summary>
        /// <param name="nvert">Number of vertices in the polygon.</param>
        /// <param name="vertx">Array containing the x coordinates of the polygon's vertices.</param>
        /// <param name="verty">Array containing the y coordinates of the polygon's vertices.</param>
        /// <param name="testx">X-coordinate of the test point.</param>
        /// <param name="testy">Y-coordinate of the test point.</param>
        private static bool PnPoly(int nvert, float[] vertx, float[] verty, float testx, float testy)
        {
            bool inside = false;
            int i = 0;
            int j = nvert - 1;
            while (i < nvert)
            {
                if ((verty[i] > testy) != (verty[j] > testy) &&
                    testx < (vertx[j] - vertx[i]) * (testy - verty[i]) / (verty[j] - verty[i]) + vertx[i])
                {
                    inside = !inside;
                }

                j = i++;
            }

            return inside;
        }
    }
}
