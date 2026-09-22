namespace Styx.CommonBot
{
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
