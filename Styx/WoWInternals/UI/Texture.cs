using System;
using System.Collections.Generic;
using System.Drawing;
using Styx.Common;

namespace Styx.WoWInternals.UI
{
    internal class Texture : LayeredRegion
    {
        public BlendMode Blend
        {
            get
            {
                Enum.TryParse<BlendMode>(Call<string>("GetBlendMode"), ignoreCase: true, out var result);
                return result;
            }
            set { Invoke("SetBlendMode", "'" + value.ToString().ToUpper() + "'"); }
        }

        public bool HorizontalTile
        {
            get { return Call<bool>("GetHorizTile"); }
            set { Invoke("SetHorizTile", value); }
        }

        public bool VerticalTile
        {
            get { return Call<bool>("GetVertTile"); }
            set { Invoke("SetVertTile", value); }
        }

        public string TexturePath => Call<string>("GetTexture");

        public bool Desaturated
        {
            get { return Call<bool>("IsDesaturated"); }
            set { Invoke("SetDesaturated", value); }
        }

        public bool IsNonBlocking
        {
            get { return Call<bool>("GetNonBlocking"); }
            set { Invoke("SetNonBlocking", value); }
        }

        public Texture(string name)
            : base(name)
        {
        }

        public void GetTexCoord(out Vector2 topLeft, out Vector2 topRight, out Vector2 bottomLeft, out Vector2 bottomRight)
        {
            topLeft = (topRight = (bottomLeft = (bottomRight = Vector2.Zero)));
            List<string> list = CallMultiValue("GetTexCoord");
            float.TryParse(list[0], out topLeft.X);
            float.TryParse(list[1], out topLeft.Y);
            float.TryParse(list[2], out topRight.X);
            float.TryParse(list[3], out topRight.Y);
            float.TryParse(list[4], out bottomLeft.X);
            float.TryParse(list[5], out bottomLeft.Y);
            float.TryParse(list[6], out bottomRight.X);
            float.TryParse(list[7], out bottomRight.Y);
        }

        public void SetTexCoord(Vector2 topLeft, Vector2 topRight, Vector2 bottomLeft, Vector2 bottomRight)
        {
            Invoke("SetTexCoord", topLeft.X, topLeft.Y, topRight.X, topRight.Y, bottomLeft.X, bottomLeft.Y, bottomRight.X, bottomRight.Y);
        }

        public void SetTexCoord(float left, float right, float top, float bottom)
        {
            Invoke("SetTexCoord", left, right, top, bottom);
        }

        public void SetRotation(float radians)
        {
            Invoke("SetRotation", radians);
        }

        public void SetGradientAlpha(Orientation orientation, Color from, Color to)
        {
            FromColor(from, out var r1, out var g1, out var b1, out var a1);
            FromColor(to, out var r2, out var g2, out var b2, out var a2);
            Invoke("SetGradientAlpha", "'" + orientation.ToString().ToUpper() + "'", r1, g1, b1, a1, r2, g2, b2, a2);
        }

        public void SetTexture(string path)
        {
            Invoke("SetTexture", "'" + path + "'");
        }

        public void SetTexture(Color color)
        {
            FromColor(color, out var r, out var g, out var b, out var a);
            Invoke("SetTexture", r, g, b, a);
        }
    }
}
