using System;
using System.Collections.Generic;
using System.Drawing;
using Styx.Common;

namespace Styx.WoWInternals.UI
{
    internal class Frame : Region
    {
        public bool CanChangeAttribute => Call<bool>("CanChangeAttribute");

        public bool IsJoystickEnabled
        {
            get { return Call<bool>("IsJoystickEnabled"); }
            set { Invoke("EnableJoystick", value); }
        }

        public bool IsKeyboardEnabled
        {
            get { return Call<bool>("IsKeyboardEnabled"); }
            set { Invoke("EnableKeyboard", value); }
        }

        public bool IsMouseEnabled
        {
            get { return Call<bool>("IsMouseEnabled"); }
            set { Invoke("EnableMouse", value); }
        }

        public bool IsMouseWheelEnabled
        {
            get { return Call<bool>("IsMouseWheelEnabled"); }
            set { Invoke("EnableMouseWheel", value); }
        }

        public Color BackdropBorderColor
        {
            get { return CallColor("GetBackdropBorderColor"); }
            set { InvokeColor("SetBackdropBorderColor", value); }
        }

        public Color BackdropColor
        {
            get { return CallColor("GetBackdropColor"); }
            set { InvokeColor("SetBackdropColor", value); }
        }

        public Rectangle BoundsRect
        {
            get
            {
                List<string> list = CallMultiValue("GetBoundsRect");
                int.TryParse(list[0], out var x);
                int.TryParse(list[1], out var y);
                int.TryParse(list[2], out var width);
                int.TryParse(list[3], out var height);
                return new Rectangle(x, y, width, height);
            }
        }

        public List<Frame> Children
        {
            get { throw new NotImplementedException(); }
        }

        public Rectangle ClampRectInsets
        {
            get
            {
                List<string> list = CallMultiValue("GetClampRectInsets");
                int.TryParse(list[0], out var x);
                int.TryParse(list[1], out var y);
                int.TryParse(list[2], out var width);
                int.TryParse(list[3], out var height);
                return new Rectangle(x, y, width, height);
            }
            set { Invoke("SetClampRectInsets", value.X, value.Y, value.Width, value.Height); }
        }

        public Rectangle HitRectInsets
        {
            get
            {
                List<string> list = CallMultiValue("GetHitRectInsets");
                int.TryParse(list[0], out var x);
                int.TryParse(list[1], out var y);
                int.TryParse(list[2], out var width);
                int.TryParse(list[3], out var height);
                return new Rectangle(x, y, width, height);
            }
            set { Invoke("SetHitRectInsets", value.X, value.Y, value.Width, value.Height); }
        }

        public float Depth
        {
            get { return Call<float>("GetDepth"); }
            set { Invoke("SetDepth", value); }
        }

        public float EffectiveAlpha => Call<float>("GetEffectiveAlpha");

        public float EffectiveDepth => Call<float>("GetEffectiveDepth");

        public float EffectiveScale => Call<float>("GetEffectiveScale");

        public int FrameLevel
        {
            get { return Call<int>("GetFrameLevel"); }
            set { Invoke("SetFrameLevel", value); }
        }

        public FrameStrata FrameStrata
        {
            get
            {
                FrameStrata result = FrameStrata.Background;
                Enum.TryParse<FrameStrata>(Call<string>("GetFrameStrata"), ignoreCase: true, out result);
                return result;
            }
            set { Invoke("SetFrameStrata", "'" + value.ToString().ToUpper() + "'"); }
        }

        public int Id
        {
            get { return Call<int>("GetID"); }
            set { Invoke("SetID", value); }
        }

        public Vector2 MaxResize
        {
            get
            {
                Vector2 result = default(Vector2);
                List<string> list = CallMultiValue("GetMaxResize");
                float.TryParse(list[0], out result.X);
                float.TryParse(list[1], out result.Y);
                return result;
            }
            set { Invoke("SetMaxResize", value.X, value.Y); }
        }

        public Vector2 MinResize
        {
            get
            {
                Vector2 result = default(Vector2);
                List<string> list = CallMultiValue("GetMinResize");
                float.TryParse(list[0], out result.X);
                float.TryParse(list[1], out result.Y);
                return result;
            }
            set { Invoke("SetMinResize", value.X, value.Y); }
        }

        public int NumChildren => Call<int>("GetNumChildren");

        public int NumRegions => Call<int>("GetNumRegions");

        public List<RegionBase> Regions
        {
            get { throw new NotImplementedException(); }
        }

        public float Scale
        {
            get { return Call<float>("GetScale"); }
            set { Invoke("SetScale", value); }
        }

        public bool IsClampedToScreen
        {
            get { return Call<bool>("IsClampedToScreen"); }
            set { Invoke("SetClampedToScreen", value); }
        }

        public bool IsIgnoringDepth
        {
            get { return Call<bool>("IsIgnoringDepth"); }
            set { Invoke("IgnoreDepth", value); }
        }

        public bool IsMovable
        {
            get { return Call<bool>("IsMovable"); }
            set { Invoke("SetMovable", value); }
        }

        public bool IsResizable
        {
            get { return Call<bool>("IsResizable"); }
            set { Invoke("SetResizable", value); }
        }

        public bool IsTopLevel
        {
            get { return Call<bool>("IsToplevel"); }
            set { Invoke("SetToplevel", value); }
        }

        public bool IsUserPlaced
        {
            get { return Call<bool>("IsUserPlaced"); }
            set { Invoke("SetUserPlaced", value); }
        }

        protected internal Frame(string name)
            : base(name)
        {
        }

        public Button CreateButton(string name, string template = "UIPanelButtonTemplate")
        {
            Lua.DoString("CreateFrame('Button', '{0}', {1}, '{2}')", name, LuaName, template);
            return new Button(name);
        }

        public CheckButton CreateCheckButton(string name, string template = "UICheckButtonTemplate")
        {
            Lua.DoString("CreateFrame('CheckButton', '{0}', {1}, '{2}')", name, LuaName, template);
            return new CheckButton(name);
        }

        public FontString CreateCheckButtonFontString(string name)
        {
            Lua.DoString("CreateFrame('CheckButton', '{0}', {1})", name, LuaName);
            return new FontString(name);
        }

        public static Frame CreateFrame(string name, string parent = "UIParent")
        {
            Lua.DoString(string.Format(name + " = CreateFrame('Frame', '{0}', {1})", name, parent));
            return new Frame(name);
        }

        public void AllowAttributeChanges()
        {
            Invoke("AllowAttributeChanges");
        }

        public FontString CreateFontString(string name, Layer layer = Layer.Artwork, string inherits = null)
        {
            Invoke("CreateFontString", "'" + name + "'", "'" + layer.ToString().ToUpper() + "'", inherits);
            return new FontString(name);
        }

        public Texture CreateTexture(string name, Layer layer = Layer.Artwork, string inherits = null)
        {
            Invoke("CreateTexture", "'" + name + "'", "'" + layer.ToString().ToUpper() + "'", inherits);
            return new Texture(name);
        }

        public RegionBase CreateTitleRegion(string name)
        {
            Lua.DoString(name + " = " + LuaName + ":CreateTitleRegion()");
            return new RegionBase(name);
        }

        public void DisableDrawLayer(Layer layer)
        {
            Invoke("DisableDrawLayer", "'" + layer.ToString().ToUpper() + "'");
        }

        public void EnableDrawLayer(Layer layer)
        {
            Invoke("EnableDrawLayer", "'" + layer.ToString().ToUpper() + "'");
        }

        public object GetScript()
        {
            throw new NotImplementedException();
        }

        public bool IsEventRegistered(string eventName)
        {
            return Call<bool>("IsEventRegistered", "'" + eventName + "'");
        }

        public void Lower()
        {
            Invoke("Lower");
        }

        public void Raise()
        {
            Invoke("Raise");
        }

        public void RegisterAllEvents()
        {
            Invoke("RegisterAllEvents");
        }

        public void RegisterEvent(string eventName)
        {
            Invoke("RegisterEvent", "'" + eventName + "'");
        }

        public void RegisterForDrag(params string[] buttons)
        {
            Invoke("RegisterForDrag", buttons);
        }

        public void SetAttribute(string name, object value)
        {
            Invoke("SetAttribute", "'" + name + "'", value);
        }

        public T GetAttribute<T>(string name)
        {
            return Call<T>("GetAttribute", "'" + name + "'");
        }

        public void SetBackdrop(Backdrop backdrop)
        {
            Invoke("SetBackdrop", backdrop.ToLuaTableString());
        }

        public void StartMoving()
        {
            Invoke("StartMoving");
        }

        public void StartSizing()
        {
            Invoke("StartSizing");
        }

        public void StopMovingOrSizing()
        {
            Invoke("StopMovingOrSizing");
        }

        public void UnregisterAllEvents()
        {
            Invoke("UnregisterAllEvents");
        }

        public void UnregisterEvent(string eventName)
        {
            Invoke("UnregisterEvent", "'" + eventName + "'");
        }

        public void SetScript(string script, string handler)
        {
            Invoke("SetScript", "'" + script + "'", handler);
        }
    }
}
