using System.Collections.Generic;
using Styx.CommonBot;

namespace Bots.Gatherbuddy
{
    public partial class GatherbuddyBot : BotBase
    {
        internal static List<Herb> herbList = new List<Herb>
        {
            new Herb(181166, "Bloodthistle", 1),
            new Herb(1621, "Briarthorn", 1),
            new Herb(1622, "Bruiseweed", 1),
            new Herb(1619, "Earthroot", 1),
            new Herb(209355, "Fool's Cap", 1),
            new Herb(221547, "Fool's Cap (Timeless Isle)", 1),
            new Herb(209354, "Golden Lotus", 1),
            new Herb(221545, "Golden Lotus (Timeless Isle)", 1),
            new Herb(209349, "Green Tea Leaf", 1),
            new Herb(221542, "Green Tea Leaf (Timeless Isle)", 1),
            new Herb(1620, "Mageroyal", 1),
            new Herb(1618, "Peacebloom", 1),
            new Herb(209353, "Rain Poppy", 1),
            new Herb(221543, "Rain Poppy (Timeless Isle)", 1),
            new Herb(214510, "Sha-Touched Herb", 1),
            new Herb(215411, "Sha-Touched Herb (Engineering Goggles)", 1),
            new Herb(215403, "Sha-Touched Herb (Engineering Goggles #2)", 1),
            new Herb(209350, "Silkweed", 1),
            new Herb(221544, "Silkweed (Timeless Isle)", 1),
            new Herb(1617, "Silverleaf", 1),
            new Herb(209351, "Snow Lily", 1),
            new Herb(215405, "Green Tea Leaf (Engineering Goggles)", 1),
            new Herb(215408, "Rain Poppy (Engineering Goggles)", 1),
            new Herb(215406, "Silkweed (Engineering Goggles)", 1),
            new Herb(215407, "Snow Lily (Engineering Goggles)", 1),
            new Herb(215410, "Fool's Cap (Engineering Goggles)", 1),
            new Herb(2045, "Stranglekelp", 85),
            new Herb(1628, "Grave Moss", 105),
            new Herb(1623, "Wild Steelbloom", 115),
            new Herb(1624, "Kingsblood", 125),
            new Herb(2042, "Fadeleaf", 150),
            new Herb(2046, "Goldthorn", 150),
            new Herb(2041, "Liferoot", 150),
            new Herb(2043, "Khadgar's Whisker", 160),
            new Herb(2044, "Dragon's Teeth", 195),
            new Herb(2866, "Firebloom", 205),
            new Herb(142140, "Purple Lotus", 210),
            new Herb(142141, "Arthas' Tears", 220),
            new Herb(142142, "Sungrass", 230),
            new Herb(142143, "Blindweed", 235),
            new Herb(142144, "Ghost Mushroom", 245),
            new Herb(142145, "Gromsblood", 250),
            new Herb(176583, "Golden Sansam", 260),
            new Herb(176584, "Dreamfoil", 270),
            new Herb(176588, "Icecap", 270),
            new Herb(176586, "Mountain Silversage", 280),
            new Herb(176587, "Sorrowmoss", 285),
            new Herb(176589, "Black Lotus", 300),
            new Herb(181270, "Felweed", 300),
            new Herb(181271, "Dreaming Glory", 315),
            new Herb(181275, "Ragveil", 325),
            new Herb(181277, "Terocone", 325),
            new Herb(181276, "Flame Cap", 335),
            new Herb(181278, "Ancient Lichen", 340),
            new Herb(189973, "Goldclover", 350),
            new Herb(181279, "Netherbloom", 350),
            new Herb(185881, "Netherdust Bush", 350),
            new Herb(191303, "Firethorn", 360),
            new Herb(181280, "Nightmare Vine", 365),
            new Herb(181281, "Mana Thistle", 375),
            new Herb(190169, "Tiger Lily", 375),
            new Herb(190170, "Talandra's Rose", 385),
            new Herb(191019, "Adder's Tongue", 400),
            new Herb(190173, "Frozen Herb", 400),
            new Herb(202749, "Azshara's Veil", 425),
            new Herb(202747, "Cinderbloom", 425),
            new Herb(190171, "Lichbloom", 425),
            new Herb(202748, "Stormvine", 425),
            new Herb(190172, "Icethorn", 435),
            new Herb(190176, "Frost Lotus", 450),
            new Herb(202750, "Heartblossom", 475),
            new Herb(202752, "Whiptail", 500),
            new Herb(202751, "Twilight Jasmine", 525)
        };

        internal static List<Mineral> mineList = new List<Mineral>
        {
            new Mineral(1731, "Copper Vein", 1),
            new Mineral(191844, "Enchanted Earth", 1),
            new Mineral(191845, "Enchanted Earth", 1),
            new Mineral(188699, "Strange Ore", 1),
            new Mineral(209311, "Ghost Iron Deposit", 1),
            new Mineral(221538, "Ghost Iron Deposit (Timeless Isle)", 1),
            new Mineral(209328, "Rich Ghost Iron Deposit", 1),
            new Mineral(221539, "Rich Ghost Iron Deposit (Timeless Isle)", 1),
            new Mineral(209312, "Kyparite Deposit", 1),
            new Mineral(209329, "Rich Kyparite Deposit", 1),
            new Mineral(209313, "Trillium Vein", 1),
            new Mineral(221541, "Trillium Vein (Timeless Isle)", 1),
            new Mineral(209330, "Rich Trillium Vein", 1),
            new Mineral(221540, "Rich Trillium Vein (Timeless Isle)", 1),
            new Mineral(1610, "Incendicite Mineral Vein", 50),
            new Mineral(1732, "Tin Vein", 50),
            new Mineral(2653, "Lesser Bloodstone Deposit", 65),
            new Mineral(73940, "Ooze Covered Silver Vein", 65),
            new Mineral(1733, "Silver Vein", 65),
            new Mineral(1735, "Iron Deposit", 100),
            new Mineral(19903, "Indurium Mineral Vein", 150),
            new Mineral(1734, "Gold Vein", 115),
            new Mineral(73941, "Ooze Covered Gold Vein", 115),
            new Mineral(2040, "Mithril Deposit", 150),
            new Mineral(123310, "Ooze Covered Mithril Deposit", 150),
            new Mineral(123309, "Ooze Covered Truesilver Deposit", 165),
            new Mineral(2047, "Truesilver Deposit", 165),
            new Mineral(165658, "Dark Iron Deposit", 175),
            new Mineral(123848, "Ooze Covered Thorium Vein", 200),
            new Mineral(324, "Small Thorium Vein", 200),
            new Mineral(177388, "Ooze Covered Rich Thorium Vein", 215),
            new Mineral(175404, "Rich Thorium Vein", 215),
            new Mineral(181555, "Fel Iron Deposit", 275),
            new Mineral(185877, "Nethercite Deposit", 275),
            new Mineral(181069, "Large Obsidian Chunk", 305),
            new Mineral(181068, "Small Obsidian Chunk", 305),
            new Mineral(181556, "Adamantite Deposit", 325),
            new Mineral(189978, "Cobalt Deposit", 350),
            new Mineral(181569, "Rich Adamantite Deposit", 350),
            new Mineral(185557, "Ancient Gem Vein", 375),
            new Mineral(181557, "Khorium Vein", 375),
            new Mineral(189979, "Rich Cobalt Deposit", 375),
            new Mineral(189980, "Saronite Deposit", 400),
            new Mineral(202736, "Obsidium Deposit", 425),
            new Mineral(189981, "Rich Saronite Deposit", 425),
            new Mineral(195036, "Pure Saronite Deposit", 450),
            new Mineral(202739, "Rich Obsidium Deposit", 450),
            new Mineral(191133, "Titanium Vein", 450),
            new Mineral(202738, "Elementium Vein", 475),
            new Mineral(202741, "Rich Elementium Vein", 500),
            new Mineral(202737, "Pyrite Deposit", 525),
            new Mineral(202740, "Rich Pyrite Deposit", 525),
            new Mineral(215418, "Trillium Vein (Engineering Goggles)", 1)
        };

        internal static List<uint> nodeAvoidList = new List<uint>
        {
            185881, 190702, 185877, 1610, 19903, 73940, 73941, 123310, 123309, 123848, 177388
        };

        internal class Herb
        {
            private readonly uint entry;
            private readonly string name;
            private readonly uint requiredSkill;

            public uint Entry { get { return entry; } }
            public string Name { get { return name; } }
            public uint RequiredSkill { get { return requiredSkill; } }

            public Herb(uint Entry, string Name, uint RequiredSkill)
            {
                entry = Entry;
                name = Name;
                requiredSkill = RequiredSkill;
            }
        }

        internal class Mineral
        {
            private readonly uint entry;
            private readonly string name;
            private readonly uint requiredSkill;

            public uint Entry { get { return entry; } }
            public string Name { get { return name; } }
            public uint RequiredSkill { get { return requiredSkill; } }

            public Mineral(uint Entry, string Name, uint RequiredSkill)
            {
                entry = Entry;
                name = Name;
                requiredSkill = RequiredSkill;
            }
        }
    }
}
