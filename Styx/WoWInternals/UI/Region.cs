namespace Styx.WoWInternals.UI
{
    internal class Region : RegionBase
    {
        public float Alpha
        {
            get { return Call<float>("GetAlpha"); }
            set { Invoke("SetAlpha", value); }
        }

        public bool IsShown
        {
            get { return Call<bool>("IsShown"); }
            set { Invoke(value ? "Show" : "Hide"); }
        }

        public bool IsVisible => Call<bool>("IsVisible");

        public Region(string name)
            : base(name)
        {
        }
    }
}
