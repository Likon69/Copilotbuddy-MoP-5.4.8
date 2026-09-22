namespace Styx.Helpers
{
    public static class WoWSpecExtensions
    {
        public static bool IsTank(this WoWSpec spec)
        {
            return spec == WoWSpec.PaladinProtection || spec == WoWSpec.WarriorProtection ||
                   spec == WoWSpec.DruidGuardian || spec == WoWSpec.DeathKnightBlood ||
                   spec == WoWSpec.MonkBrewmaster;
        }

        public static bool IsHealer(this WoWSpec spec)
        {
            return spec == WoWSpec.PaladinHoly || spec == WoWSpec.DruidRestoration ||
                   spec == WoWSpec.PriestDiscipline || spec == WoWSpec.PriestHoly ||
                   spec == WoWSpec.ShamanRestoration || spec == WoWSpec.MonkMistweaver;
        }

        public static bool IsDps(this WoWSpec spec)
        {
            return !spec.IsTank() && !spec.IsHealer();
        }

        public static bool IsRange(this WoWSpec spec)
        {
            switch (spec)
            {
                case WoWSpec.MageArcane:
                case WoWSpec.MageFire:
                case WoWSpec.MageFrost:
                case WoWSpec.PaladinHoly:
                case WoWSpec.DruidBalance:
                case WoWSpec.DruidRestoration:
                case WoWSpec.HunterBeastMastery:
                case WoWSpec.HunterMarksmanship:
                case WoWSpec.HunterSurvival:
                case WoWSpec.PriestDiscipline:
                case WoWSpec.PriestHoly:
                case WoWSpec.PriestShadow:
                case WoWSpec.ShamanElemental:
                case WoWSpec.ShamanRestoration:
                case WoWSpec.WarlockAffliction:
                case WoWSpec.WarlockDemonology:
                case WoWSpec.WarlockDestruction:
                case WoWSpec.MonkMistweaver:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsMelee(this WoWSpec spec)
        {
            return !spec.IsRange();
        }
    }
}
