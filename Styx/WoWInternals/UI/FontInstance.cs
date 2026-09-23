using System;
using System.Collections.Generic;
using System.Drawing;
using Styx.Common;

namespace Styx.WoWInternals.UI
{
    internal class FontInstance : UIObject
    {
        public float JustifyHorizontal
        {
            get { return Call<float>("GetJustifyH"); }
            set { Invoke("SetJustifyH", value); }
        }

        public float JustifyVertical
        {
            get { return Call<float>("GetJustifyV"); }
            set { Invoke("SetJustifyV", value); }
        }

        public float LineSpacing
        {
            get { return Call<float>("GetSpacing"); }
            set { Invoke("SetSpacing", value); }
        }

        public Color ShadowColor
        {
            get { return GetShadowColor(); }
            set { SetShadowColor(value); }
        }

        public Color TextColor
        {
            get { return GetTextColor(); }
            set { SetTextColor(value); }
        }

        public Vector2 ShadowOffset
        {
            get
            {
                List<string> list = CallMultiValue("GetShadowOffset");
                float.TryParse(list[0], out var x);
                float.TryParse(list[1], out var y);
                return new Vector2(x, y);
            }
            set { Invoke("SetShadowOffset", value.X, value.Y); }
        }

        protected internal FontInstance(string name)
            : base(name)
        {
        }

        public void GetFont(out string fontFile, out float height, out FontFlags flags)
        {
            List<string> list = CallMultiValue("GetFont");
            fontFile = list[0];
            float.TryParse(list[1], out height);
            flags = FontFlags.None;
            string[] array = list[2].Split(',');
            foreach (string text in array)
            {
                Enum.TryParse<FontFlags>(text.Trim(), ignoreCase: true, out var result);
                flags |= result;
            }
        }

        public object GetFontObject()
        {
            return Call<object>("GetFontObject");
        }

        private Color GetShadowColor()
        {
            List<string> list = CallMultiValue("GetShadowColor");
            float.TryParse(list[0], out var r);
            float.TryParse(list[1], out var g);
            float.TryParse(list[2], out var b);
            float.TryParse(list[3], out var a);
            return ToColor(r, g, b, a);
        }

        private void SetShadowColor(Color color)
        {
            FromColor(color, out var r, out var g, out var b, out var a);
            Invoke("SetShadowColor", r, g, b, a);
        }

        private Color GetTextColor()
        {
            List<string> list = CallMultiValue("GetTextColor");
            float.TryParse(list[0], out var r);
            float.TryParse(list[1], out var g);
            float.TryParse(list[2], out var b);
            float.TryParse(list[3], out var a);
            return ToColor(r, g, b, a);
        }

        private void SetTextColor(Color color)
        {
            FromColor(color, out var r, out var g, out var b, out var a);
            Invoke("SetTextColor", r, g, b, a);
        }
    }
}
