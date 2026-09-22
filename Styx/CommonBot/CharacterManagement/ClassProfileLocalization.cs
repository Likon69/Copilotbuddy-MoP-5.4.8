using System;
using Styx.Localization;

namespace Styx.CommonBot.CharacterManagement
{
    public static class ClassProfileLocalization
    {
        public static string GetClassName(WoWClass cls)
        {
            switch (cls)
            {
            case WoWClass.Warrior:
                return Globalization.Warrior;
            case WoWClass.Paladin:
                return Globalization.Paladin;
            case WoWClass.Hunter:
                return Globalization.Hunter;
            case WoWClass.Rogue:
                return Globalization.Rogue;
            case WoWClass.Priest:
                return Globalization.Priest;
            case WoWClass.DeathKnight:
                return Globalization.DeathKnight;
            case WoWClass.Shaman:
                return Globalization.Shaman;
            case WoWClass.Mage:
                return Globalization.Mage;
            case WoWClass.Warlock:
                return Globalization.Warlock;
            case WoWClass.Monk:
                return Globalization.Monk;
            case WoWClass.Druid:
                return Globalization.Druid;
            default:
                return Globalization.Unknown;
            }
        }

        public static string GetSpecName(WoWSpec spec)
        {
            if (spec <= WoWSpec.PetTenacity)
            {
                if (spec == WoWSpec.None)
                {
                    return Globalization.NoSpec;
                }
                switch (spec)
                {
                case WoWSpec.MageArcane:
                    return Globalization.MageArcane;
                case WoWSpec.MageFire:
                    return Globalization.MageFire;
                case WoWSpec.MageFrost:
                    return Globalization.MageFrost;
                case WoWSpec.PaladinHoly:
                    return Globalization.PaladinHoly;
                case WoWSpec.PaladinProtection:
                    return Globalization.PaladinProtection;
                case WoWSpec.PaladinRetribution:
                    return Globalization.PaladinRetribution;
                case WoWSpec.WarriorArms:
                    return Globalization.WarriorArms;
                case WoWSpec.WarriorFury:
                    return Globalization.WarriorFury;
                case WoWSpec.WarriorProtection:
                    return Globalization.WarriorProtection;
                case WoWSpec.PetFerocity:
                    return Globalization.PetFerocity;
                case WoWSpec.PetCunning:
                    return Globalization.PetCunning;
                case WoWSpec.PetTenacity:
                    return Globalization.PetTenacity;
                }
            }
            else
            {
                switch (spec)
                {
                case WoWSpec.DruidBalance:
                    return Globalization.DruidBalance;
                case WoWSpec.DruidFeral:
                    return Globalization.DruidFeral;
                case WoWSpec.DruidGuardian:
                    return Globalization.DruidGuardian;
                case WoWSpec.DruidRestoration:
                    return Globalization.DruidRestoration;
                default:
                    switch (spec)
                    {
                    case WoWSpec.DeathKnightBlood:
                        return Globalization.DeathKnightBlood;
                    case WoWSpec.DeathKnightFrost:
                        return Globalization.DeathKnightFrost;
                    case WoWSpec.DeathKnightUnholy:
                        return Globalization.DeathKnightUnholy;
                    case WoWSpec.HunterBeastMastery:
                        return Globalization.HunterBeastMastery;
                    case WoWSpec.HunterMarksmanship:
                        return Globalization.HunterMarksmanship;
                    case WoWSpec.HunterSurvival:
                        return Globalization.HunterSurvival;
                    case WoWSpec.PriestDiscipline:
                        return Globalization.PriestDiscipline;
                    case WoWSpec.PriestHoly:
                        return Globalization.PriestHoly;
                    case WoWSpec.PriestShadow:
                        return Globalization.PriestShadow;
                    case WoWSpec.RogueAssassination:
                        return Globalization.RogueAssassination;
                    case WoWSpec.RogueCombat:
                        return Globalization.RogueCombat;
                    case WoWSpec.RogueSubtlety:
                        return Globalization.RogueSubtlety;
                    case WoWSpec.ShamanElemental:
                        return Globalization.ShamanElemental;
                    case WoWSpec.ShamanEnhancement:
                        return Globalization.ShamanEnhancement;
                    case WoWSpec.ShamanRestoration:
                        return Globalization.ShamanRestoration;
                    case WoWSpec.WarlockAffliction:
                        return Globalization.WarlockAffliction;
                    case WoWSpec.WarlockDemonology:
                        return Globalization.WarlockDemonology;
                    case WoWSpec.WarlockDestruction:
                        return Globalization.WarlockDestruction;
                    case WoWSpec.MonkBrewmaster:
                        return Globalization.MonkBrewmaster;
                    case WoWSpec.MonkWindwalker:
                        return Globalization.MonkWindwalker;
                    case WoWSpec.MonkMistweaver:
                        return Globalization.MonkMistweaver;
                    }
                    break;
                }
            }
            return Globalization.Unknown;
        }
    }
}
