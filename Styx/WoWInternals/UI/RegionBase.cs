using System;
using System.Collections.Generic;
using Styx.Common;

namespace Styx.WoWInternals.UI
{
    internal class RegionBase : UIObject
    {
        public bool IsProtected => Call<bool>("IsProtected");

        public bool CanChangeProtectedState => Call<bool>("CanChangeProtectedState");

        public float Top => Call<float>("GetTop");

        public float Bottom => Call<float>("GetBottom");

        public float Center => Call<float>("GetCenter");

        public float Left => Call<float>("GetLeft");

        public float Right => Call<float>("GetRight");

        public float Height
        {
            get { return Call<float>("GetHeight"); }
            set { Invoke("SetHeight", value); }
        }

        public float Width
        {
            get { return Call<float>("GetWidth"); }
            set { Invoke("SetWidth", value); }
        }

        public Vector2 Size
        {
            get
            {
                List<string> list = CallMultiValue("GetSize");
                float x = float.Parse(list[0]);
                float y = float.Parse(list[1]);
                return new Vector2(x, y);
            }
            set { Invoke("SetSize", value.X, value.Y); }
        }

        public int NumPoints => Call<int>("GetNumPoints");

        public bool IsDragging => Call<bool>("IsDragging");

        public bool IsMouseOver => Call<bool>("IsMouseOver");

        protected internal RegionBase(string name)
            : base(name)
        {
        }

        public void SetParent(string name)
        {
            Invoke("SetParent", name);
        }

        public void SetSize(float width, float height)
        {
            Invoke("SetSize", width, height);
        }

        public void GetPoint(int index, out AnchorPoint point, out object relativeTo, out AnchorPoint relativePoint, out int x, out int y)
        {
            List<string> list = CallMultiValue("GetPoint", index);
            Enum.TryParse<AnchorPoint>(list[0], ignoreCase: true, out point);
            relativeTo = null;
            Enum.TryParse<AnchorPoint>(list[2], ignoreCase: true, out relativePoint);
            int.TryParse(list[3], out x);
            int.TryParse(list[4], out y);
        }

        public void SetPoint(AnchorPoint point)
        {
            Invoke("SetPoint", "'" + point.ToString().ToUpper() + "'");
        }

        public void SetPoint(AnchorPoint point, string relativeTo, AnchorPoint relativePoint, int x, int y)
        {
            Invoke("SetPoint", "'" + point.ToString().ToUpper() + "'", relativeTo, "'" + relativePoint.ToString().ToUpper() + "'", x, y);
        }

        public void SetAllPoints(string relativeTo = null)
        {
            Invoke("SetAllPoints", relativeTo);
        }

        public void ClearAllPoints()
        {
            Invoke("ClearAllPoints");
        }
    }
}
