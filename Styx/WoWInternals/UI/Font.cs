namespace Styx.WoWInternals.UI
{
    internal class Font : FontInstance
    {
        public float Alpha
        {
            get { return Call<float>("GetAlpha"); }
            set { Invoke("SetAlpha", value); }
        }

        public float IndentedWordWrap
        {
            get { return Call<float>("GetIndentedWordWrap"); }
            set { Invoke("SetIndentedWordWrap", value); }
        }

        public Font(string name)
            : base(name)
        {
        }

        public void CopyFontObject(string name)
        {
            Invoke("CopyFontObject", name);
        }
    }
}
