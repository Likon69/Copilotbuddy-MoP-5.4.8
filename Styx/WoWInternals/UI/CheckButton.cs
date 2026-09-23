using Styx.Common;

namespace Styx.WoWInternals.UI
{
    internal class CheckButton : Button
    {
        public static class Templates
        {
            public const string UICheckButtonTemplate = "UICheckButtonTemplate";

            public const string InterfaceOptionsCheckButtonTemplate = "InterfaceOptionsCheckButtonTemplate";

            public const string ActionBarButtonTemplate = "ActionBarButtonTemplate";
        }

        public bool IsChecked
        {
            get { return Call<bool>("GetChecked"); }
            set { Invoke("SetChecked", value); }
        }

        public Texture CheckedTexture => new Texture(Lua.GetReturnVal<string>(LuaName + ":GetCheckedTexture():GetName()", 0u));

        public Texture DisabledCheckedTexture => new Texture(Lua.GetReturnVal<string>(LuaName + ":GetDisabledCheckedTexture():GetName()", 0u));

        public override string Text
        {
            get { return base.Text; }
            set { Lua.DoString("getglobal(" + LuaName + ":GetName() .. 'Text'):SetText(\"" + value + "\")"); }
        }

        protected internal CheckButton(string name)
            : base(name)
        {
        }

        public void SetCheckedTexture(Texture texture)
        {
            Invoke("SetCheckedTexture", texture.LuaName);
        }

        public void SetCheckedTexture(string path)
        {
            Invoke("SetCheckedTexture", "\"" + path + "\"");
        }

        public void SetDisabledCheckedTexture(Texture texture)
        {
            Invoke("SetDisabledCheckedTexture", texture.LuaName);
        }

        public void SetDisabledCheckedTexture(string path)
        {
            Invoke("SetDisabledCheckedTexture", "\"" + path + "\"");
        }
    }
}
