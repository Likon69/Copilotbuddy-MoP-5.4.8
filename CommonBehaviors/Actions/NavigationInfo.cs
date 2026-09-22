using Styx;

namespace CommonBehaviors.Actions
{
    public class NavigationInfo
    {
        public NavigationInfo(WoWPoint destination, float height)
        {
            Destination = destination;
            Height = height;
        }

        public WoWPoint Destination { get; private set; }
        public float Height { get; private set; }
    }
}
