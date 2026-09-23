namespace Styx.WoWInternals.UI
{
    internal class FontString : LayeredRegion
    {
        public FontInstance FontInstance { get; private set; }

        public string Text
        {
            get { return Call<string>("GetText"); }
            set { Invoke("SetText", "\"" + value + "\""); }
        }

        public bool NonSpaceWrap
        {
            get { return Call<bool>("CanNonSpaceWrap"); }
            set { Invoke("SetNonSpaceWrap", value); }
        }

        public bool WordWrap
        {
            get { return Call<bool>("CanWordWrap"); }
            set { Invoke("SetWordWrap", value); }
        }

        public float GetFieldSize => Call<float>("GetFieldSize");

        public float IndentedWordWrap
        {
            get { return Call<float>("GetIndentedWordWrap"); }
            set { Invoke("SetIndentedWordWrap", value); }
        }

        public float StringHeight => Call<float>("GetStringHeight");

        public float StringWidth => Call<float>("GetStringWidth");

        public float WrappedWidth => Call<float>("GetWrappedWidth");

        public bool IsTruncated => Call<bool>("IsTruncated");

        public int MaxLines
        {
            get { return Call<int>("GetMaxLines"); }
            set { Invoke("SetMaxLines", value); }
        }

        public FontString(string name)
            : base(name)
        {
            FontInstance = new FontInstance(name);
        }

        public void SetAlphaGradient(int startIndex, int endIndex)
        {
            Invoke("SetAlphaGradient", startIndex, endIndex);
        }

        public void SetTextHeight(int height)
        {
            Invoke("SetTextHeight", height);
        }
    }
}
