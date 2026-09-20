using System;
using System.Collections.Generic;
using System.Globalization;
using Styx.Common;
using Styx.WoWInternals.DBC;

namespace Styx.WoWInternals.WoWObjects
{
    public class ItemStats
    {
        private ItemStats(Dictionary<StatType, float> stats)
        {
            Stats = stats;
        }

        public Dictionary<StatType, float> Stats { get; private set; }

        public float DPS
        {
            get
            {
                float num;
                if (!Stats.TryGetValue(StatType.DamagePerSecond, out num))
                {
                    return 0f;
                }
                return num;
            }
        }

        private static string FixItemLinkLevel(string itemLink, ItemInfo itemInfo)
        {
            Styx.WoWInternals.DBC.ScalingStatDistribution scalingStatDistribution = itemInfo.ScalingStatDistribution;
            if (scalingStatDistribution == null)
            {
                return itemLink;
            }
            if (!itemLink.Contains("Hitem:"))
            {
                return itemLink;
            }
            string[] array = itemLink.Split(new char[] { ':' });
            if (array.Length <= 9)
            {
                return itemLink;
            }
            int num;
            if (!int.TryParse(array[9], NumberStyles.Integer, CultureInfo.InvariantCulture, out num))
            {
                return itemLink;
            }
            if (num < 0 || num > 100)
            {
                Styx.Common.Logging.WriteDiagnostic("Maintenance problem: Item string changed, can't fix item link");
                return itemLink;
            }
            if (num <= scalingStatDistribution.MaxLevel)
            {
                return itemLink;
            }
            array[9] = scalingStatDistribution.MaxLevel.ToString(CultureInfo.InvariantCulture);
            return string.Join(":", array);
        }

        public static ItemStats FromLink(string itemLink)
        {
            ItemInfo itemInfo = ItemInfo.FromLink(itemLink);
            if (itemInfo == null)
            {
                return null;
            }
            string text = ItemStats.FixItemLinkLevel(itemLink, itemInfo);
            List<string> returnValues = Lua.GetReturnValues(string.Format("local t = {{}} for k,v in pairs(GetItemStats([====[{0}]====])) do if _G[k] then table.insert(t, k) table.insert(t, v) end end return unpack(t)", text));
            Dictionary<StatType, float> dictionary = new Dictionary<StatType, float>();
            for (int i = 0; i < returnValues.Count; i += 2)
            {
                StatType statType;
                float num;
                if (ItemStats._statNameMap.TryGetValue(returnValues[i], out statType) && float.TryParse(returnValues[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out num))
                {
                    dictionary[statType] = num;
                }
            }
            return new ItemStats(dictionary);
        }

        private static readonly Dictionary<string, StatType> _statNameMap = new Dictionary<string, StatType>
        {
            {
                "ITEM_MOD_DAMAGE_PER_SECOND_SHORT",
                StatType.DamagePerSecond
            },
            {
                "RESISTANCE0_NAME",
                StatType.Armor
            },
            {
                "RESISTANCE1_NAME",
                StatType.HolyResistance
            },
            {
                "RESISTANCE2_NAME",
                StatType.FireResistance
            },
            {
                "RESISTANCE3_NAME",
                StatType.NatureResistance
            },
            {
                "RESISTANCE4_NAME",
                StatType.FrostResistance
            },
            {
                "RESISTANCE5_NAME",
                StatType.ShadowResistance
            },
            {
                "RESISTANCE6_NAME",
                StatType.ArcaneResistance
            },
            {
                "ITEM_MOD_ATTACK_POWER_SHORT",
                StatType.AttackPower
            },
            {
                "ITEM_MOD_MELEE_ATTACK_POWER_SHORT",
                StatType.MeleeAttackPower
            },
            {
                "ITEM_MOD_RANGED_ATTACK_POWER_SHORT",
                StatType.RangedAttackPower
            },
            {
                "ITEM_MOD_FERAL_ATTACK_POWER_SHORT",
                StatType.AttackPowerInForms
            },
            {
                "ITEM_MOD_MANA_SHORT",
                StatType.Mana
            },
            {
                "ITEM_MOD_HEALTH_SHORT",
                StatType.Health
            },
            {
                "ITEM_MOD_AGILITY_SHORT",
                StatType.Agility
            },
            {
                "ITEM_MOD_STRENGTH_SHORT",
                StatType.Strength
            },
            {
                "ITEM_MOD_INTELLECT_SHORT",
                StatType.Intellect
            },
            {
                "ITEM_MOD_SPIRIT_SHORT",
                StatType.Spirit
            },
            {
                "ITEM_MOD_STAMINA_SHORT",
                StatType.Stamina
            },
            {
                "ITEM_MOD_DEFENSE_SKILL_RATING_SHORT",
                StatType.Defense
            },
            {
                "ITEM_MOD_DODGE_RATING_SHORT",
                StatType.Dodge
            },
            {
                "ITEM_MOD_PARRY_RATING_SHORT",
                StatType.Parry
            },
            {
                "ITEM_MOD_BLOCK_RATING_SHORT",
                StatType.Block
            },
            {
                "ITEM_MOD_HIT_MELEE_RATING_SHORT",
                StatType.HitMelee
            },
            {
                "ITEM_MOD_HIT_RANGED_RATING_SHORT",
                StatType.HitRanged
            },
            {
                "ITEM_MOD_HIT_SPELL_RATING_SHORT",
                StatType.HitSpell
            },
            {
                "ITEM_MOD_CRIT_MELEE_RATING_SHORT",
                StatType.CriticalStrikeMelee
            },
            {
                "ITEM_MOD_CRIT_RANGED_RATING_SHORT",
                StatType.CriticalStrikeRanged
            },
            {
                "ITEM_MOD_CRIT_SPELL_RATING_SHORT",
                StatType.CriticalStrikeSpell
            },
            {
                "ITEM_MOD_HIT_TAKEN_MELEE_RATING_SHORT",
                StatType.HitAvoidanceMelee
            },
            {
                "ITEM_MOD_HIT_TAKEN_RANGED_RATING_SHORT",
                StatType.HitAvoidanceRanged
            },
            {
                "ITEM_MOD_HIT_TAKEN_SPELL_RATING_SHORT",
                StatType.HitAvoidanceSpell
            },
            {
                "ITEM_MOD_CRIT_TAKEN_MELEE_RATING_SHORT",
                StatType.CriticalStrikeAvoidanceMelee
            },
            {
                "ITEM_MOD_CRIT_TAKEN_RANGED_RATING_SHORT",
                StatType.CriticalStrikeAvoidanceRanged
            },
            {
                "ITEM_MOD_CRIT_TAKEN_SPELL_RATING_SHORT",
                StatType.CriticalStrikeAvoidanceSpell
            },
            {
                "ITEM_MOD_HASTE_MELEE_RATING_SHORT",
                StatType.HasteMelee
            },
            {
                "ITEM_MOD_HASTE_RANGED_RATING_SHORT",
                StatType.HasteRanged
            },
            {
                "ITEM_MOD_HASTE_SPELL_RATING_SHORT",
                StatType.HasteSpell
            },
            {
                "ITEM_MOD_HIT_RATING_SHORT",
                StatType.Hit
            },
            {
                "ITEM_MOD_CRIT_RATING_SHORT",
                StatType.CriticalStrike
            },
            {
                "ITEM_MOD_HIT_TAKEN_RATING_SHORT",
                StatType.HitAvoidance
            },
            {
                "ITEM_MOD_CRIT_TAKEN_RATING_SHORT",
                StatType.CriticalStrikeAvoidance
            },
            {
                "ITEM_MOD_RESILIENCE_RATING_SHORT",
                StatType.PvPResilience
            },
            {
                "ITEM_MOD_HASTE_RATING_SHORT",
                StatType.Haste
            },
            {
                "ITEM_MOD_EXPERTISE_RATING_SHORT",
                StatType.Expertise
            },
            {
                "ITEM_MOD_SPELL_HEALING_DONE_SHORT",
                StatType.BonusHealing
            },
            {
                "ITEM_MOD_SPELL_DAMAGE_DONE_SHORT",
                StatType.BonusDamage
            },
            {
                "ITEM_MOD_MANA_REGENERATION_SHORT",
                StatType.ManaRegeneration
            },
            {
                "ITEM_MOD_ARMOR_PENETRATION_RATING_SHORT",
                StatType.ArmorPenetration
            },
            {
                "ITEM_MOD_SPELL_POWER_SHORT",
                StatType.SpellPower
            },
            {
                "ITEM_MOD_HEALTH_REGEN_SHORT",
                StatType.HealthPer5Sec
            },
            {
                "ITEM_MOD_SPELL_PENETRATION_SHORT",
                StatType.SpellPenetration
            },
            {
                "ITEM_MOD_BLOCK_VALUE_SHORT",
                StatType.BlockValue
            },
            {
                "ITEM_MOD_MASTERY_RATING_SHORT",
                StatType.Mastery
            },
            {
                "ITEM_MOD_PVP_POWER_SHORT",
                StatType.PvPPower
            },
            {
                "ITEM_MOD_POWER_REGEN0_SHORT",
                StatType.ManaPer5Sec
            },
            {
                "ITEM_MOD_POWER_REGEN1_SHORT",
                StatType.RagePer5Sec
            },
            {
                "ITEM_MOD_POWER_REGEN2_SHORT",
                StatType.FocusPer5Sec
            },
            {
                "ITEM_MOD_POWER_REGEN3_SHORT",
                StatType.EnergyPer5Sec
            },
            {
                "ITEM_MOD_POWER_REGEN4_SHORT",
                StatType.HappinessPer5Sec
            },
            {
                "ITEM_MOD_POWER_REGEN5_SHORT",
                StatType.RunesPer5Sec
            },
            {
                "ITEM_MOD_POWER_REGEN6_SHORT",
                StatType.RunicPowerPer5Sec
            },
            {
                "EMPTY_SOCKET_META",
                StatType.MetaSocket
            },
            {
                "EMPTY_SOCKET_RED",
                StatType.RedSocket
            },
            {
                "EMPTY_SOCKET_YELLOW",
                StatType.YellowSocket
            },
            {
                "EMPTY_SOCKET_BLUE",
                StatType.BlueSocket
            },
            {
                "EMPTY_SOCKET_HYDRAULIC",
                StatType.ShaTouchedSocket
            },
            {
                "EMPTY_SOCKET_COGWHEEL",
                StatType.CogwheelSocket
            },
            {
                "EMPTY_SOCKET_PRISMATIC",
                StatType.PrismaticSocket
            }
        };
    }
}
