using System;
using System.Linq;
using Styx.Common;

namespace Styx.WoWInternals.UI
{
    internal class Button : Frame
    {
        public ButtonState ButtonState
        {
            get
            {
                Enum.TryParse<ButtonState>(Call<string>("GetButtonState"), ignoreCase: true, out var result);
                return result;
            }
        }

        public string Tooltip
        {
            get { return Lua.GetReturnVal<string>("return " + LuaName + ".tooltip", 0u); }
            set { Lua.DoString(LuaName + ".tooltip = \"" + value + "\""); }
        }

        public Font DisabledFontObject
        {
            get { return new Font(Lua.GetReturnVal<string>(LuaName + ":GetDisabledFontObject():GetName()", 0u)); }
            set { Invoke("SetDisabledFontObject", value.LuaName); }
        }

        public Texture DisabledTextureObject
        {
            get { return new Texture(Lua.GetReturnVal<string>(LuaName + ":GetDisabledTextureObject():GetName()", 0u)); }
            set { Invoke("SetDisabledTextureObject", value.LuaName); }
        }

        public FontString FontString
        {
            get { return new FontString(Lua.GetReturnVal<string>(LuaName + ":GetFontString():GetName()", 0u)); }
            set { Invoke("SetFontString", value.LuaName); }
        }

        public Font HighlightedFontObject
        {
            get { return new Font(Lua.GetReturnVal<string>(LuaName + ":GetHighlightedFontObject():GetName()", 0u)); }
            set { Invoke("SetHighlightedFontObject", value.LuaName); }
        }

        public Font NormalFontObject
        {
            get { return new Font(Lua.GetReturnVal<string>(LuaName + ":GetNormalFontObject():GetName()", 0u)); }
            set { Invoke("SetNormalFontObject", value.LuaName); }
        }

        public Texture HighlightedTexture => new Texture(Lua.GetReturnVal<string>(LuaName + ":GetHighlightedTexture():GetName()", 0u));

        public Texture NormalTexture => new Texture(Lua.GetReturnVal<string>(LuaName + ":GetNormalTexture():GetName()", 0u));

        public Texture PushedTexture => new Texture(Lua.GetReturnVal<string>(LuaName + ":GetPushedTexture():GetName()", 0u));

        public bool MotionScriptsWhileDisabled
        {
            get { return Call<bool>("GetMotionScriptsWhileDisabled"); }
            set { Invoke("SetMotionScriptsWhileDisabled", value); }
        }

        public Vector2 PushedTextOffset
        {
            get
            {
                Vector2 result = default(Vector2);
                var list = CallMultiValue("GetPushedTextOffset");
                float.TryParse(list[0], out result.X);
                float.TryParse(list[1], out result.Y);
                return result;
            }
            set { Invoke("SetPushedTextOffset", value.X, value.Y); }
        }

        public virtual string Text
        {
            get { return Call<string>("GetText"); }
            set { Invoke("SetText", "\"" + value + "\""); }
        }

        public int TextHeight => Call<int>("GetTextHeight");

        public int TextWidth => Call<int>("GetTextWidth");

        public bool IsEnabled
        {
            get { return Call<bool>("IsEnabled"); }
            set { Invoke("SetEnabled", value); }
        }

        protected internal Button(string name)
            : base(name)
        {
        }

        public void Click(InputMouseButton button = InputMouseButton.Left, bool down = true)
        {
            if (button == InputMouseButton.Left && down)
            {
                Invoke("Click");
                return;
            }
            Invoke("Click", button, down);
        }

        public void Disable()
        {
            Invoke("Disable");
        }

        public void Enable()
        {
            Invoke("Enable");
        }

        public void LockHighlight()
        {
            Invoke("LockHighlight");
        }

        public void UnlockHighlight()
        {
            Invoke("UnlockHighlight");
        }

        public void RegisterForClicks(params ButtonClickAction[] actions)
        {
            Invoke("RegisterForClicks", actions.Select((ButtonClickAction action) => string.Concat("'", action, "'")).Cast<object>().ToArray());
        }

        public void SetButtonState(ButtonState state, bool lockState)
        {
            Invoke("SetButtonState", "'" + state.ToString().ToUpper() + "'", lockState);
        }

        public void SetHighlightedTexture(Texture texture, BlendMode blend = BlendMode.Add)
        {
            Invoke("SetHighlightedTexture", texture.LuaName, "'" + blend.ToString().ToUpper() + "'");
        }

        public void SetHighlightedTexture(string path, BlendMode blend = BlendMode.Add)
        {
            Invoke("SetHighlightedTexture", "\"" + path + "\"", "'" + blend.ToString().ToUpper() + "'");
        }

        public void SetNormalTexture(Texture texture)
        {
            Invoke("SetNormalTexture", texture.LuaName);
        }

        public void SetNormalTexture(string path)
        {
            Invoke("SetNormalTexture", "\"" + path + "\"");
        }

        public void SetPushedTexture(Texture texture)
        {
            Invoke("SetPushedTexture", texture.LuaName);
        }

        public void SetPushedTexture(string path)
        {
            Invoke("SetPushedTexture", "\"" + path + "\"");
        }
    }
}
