namespace Styx.WoWInternals
{
    public class SpellCooldownInfo
    {
        public int CooldownTimeRemaining { get; set; }
        public int StartTime { get; set; }
        public int EndTime { get; set; }
        public bool CooldownBeginsWhenUsed { get; set; }
    }
}
