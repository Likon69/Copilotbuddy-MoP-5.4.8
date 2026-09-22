namespace Styx.CommonBot
{
    public enum FlightPathReason
    {
        None,
        Learn,
        Use,
        Update
    }

    public sealed class XmlFlightNode
    {
        public string Name { get; set; }
        public WoWPoint Location { get; set; }
    }

    public static class FlightPaths
    {
        public static FlightPathReason Reason { get; set; }
        public static XmlFlightNode TakingPathTo { get; set; }
        public static XmlFlightNode TakingPathFrom { get; set; }

        public static void Reset()
        {
            TakingPathFrom = null;
            TakingPathTo = null;
            Reason = FlightPathReason.None;
        }
    }
}
