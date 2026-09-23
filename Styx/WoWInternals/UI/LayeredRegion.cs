using System;
using System.Collections.Generic;
using System.Drawing;
using Styx.Common;

namespace Styx.WoWInternals.UI
{
    internal class LayeredRegion : Region
    {
        public LayeredRegion(string name)
            : base(name)
        {
        }

        public Layer GetDrawLayer()
        {
            Enum.TryParse<Layer>(Call<string>("GetDrawLayer()"), ignoreCase: true, out var result);
            return result;
        }

        public void GetDrawLayer(out Layer layer, out int subLevel)
        {
            List<string> list = CallMultiValue("GetDrawLayer()");
            Enum.TryParse<Layer>(list[0], ignoreCase: true, out layer);
            int.TryParse(list[1], out subLevel);
        }

        public void SetDrawLayer(Layer layer, int subLevel = 0)
        {
            subLevel = (int)MathEx.Clamp(-8f, 7f, subLevel);
            Invoke("SetDrawLayer", "'" + layer.ToString().ToUpper() + "'", subLevel);
        }

        public void SetVertexColor(Color color)
        {
            FromColor(color, out var r, out var g, out var b, out var a);
            Invoke("SetVertexColor", r, g, b, a);
        }
    }
}
