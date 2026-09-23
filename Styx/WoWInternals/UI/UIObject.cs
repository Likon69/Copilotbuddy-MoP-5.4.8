using System.Collections.Generic;
using System.Drawing;
using Styx.Common;
using Styx.WoWInternals;

namespace Styx.WoWInternals.UI
{
    internal abstract class UIObject
    {
        public string LuaName { get; set; }

        public string Name => Call<string>("GetName");

        public string ObjectType => Call<string>("GetObjectType");

        protected UIObject(string name)
        {
            LuaName = name;
        }

        public string GetParent()
        {
            return Call<string>("GetParent");
        }

        public bool IsObjectType(string objectType)
        {
            return Call<bool>("IsObjectType", "'" + objectType + "'");
        }

        protected List<string> CallMultiValue(string method, params object[] args)
        {
            string arg = null;
            if (args != null && args.Length > 0)
            {
                arg = string.Join(", ", args).Trim().Trim(',').Trim();
            }
            return Lua.GetReturnValues($"return {LuaName}:{method}({arg})");
        }

        protected T Call<T>(string method, params object[] args)
        {
            string arg = null;
            if (args != null && args.Length > 0)
            {
                arg = string.Join(", ", args).Trim().Trim(',').Trim();
            }
            return Lua.GetReturnVal<T>($"return {LuaName}:{method}({arg})", 0u);
        }

        protected void Invoke(string method, params object[] args)
        {
            string arg = null;
            if (args != null && args.Length > 0)
            {
                List<string> list = new List<string>();
                foreach (object obj in args)
                {
                    if (obj != null)
                    {
                        string item = obj.ToString();
                        if (obj is bool)
                        {
                            item = obj.ToString().ToLower();
                        }
                        list.Add(item);
                    }
                }
                arg = string.Join(", ", list).Trim().Trim(',').Trim();
            }
            Lua.DoString($"{LuaName}:{method}({arg})");
        }

        protected void Invoke(string method)
        {
            Invoke(method, (object[])null);
        }

        protected Color CallColor(string method, params object[] args)
        {
            List<string> list = CallMultiValue(method, args);
            float.TryParse(list[0], out var r);
            float.TryParse(list[1], out var g);
            float.TryParse(list[2], out var b);
            float.TryParse(list[3], out var a);
            return ToColor(r, g, b, a);
        }

        protected static Color ToColor(float r, float g, float b, float a)
        {
            r *= 255f;
            g *= 255f;
            b *= 255f;
            a *= 255f;
            return Color.FromArgb((int)r, (int)g, (int)b, (int)a);
        }

        protected static void FromColor(Color color, out float r, out float g, out float b, out float a)
        {
            r = MathEx.Clamp(0f, 1f, (float)(int)color.R / 255f);
            g = MathEx.Clamp(0f, 1f, (float)(int)color.G / 255f);
            b = MathEx.Clamp(0f, 1f, (float)(int)color.B / 255f);
            a = MathEx.Clamp(0f, 1f, (float)(int)color.A / 255f);
        }

        protected void InvokeColor(string method, Color color)
        {
            FromColor(color, out var r, out var g, out var b, out var a);
            Invoke(method, r, g, b, a);
        }
    }
}
