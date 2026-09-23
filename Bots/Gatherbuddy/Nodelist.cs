using System.Collections.Generic;
using Styx;
using Styx.CommonBot;

namespace Bots.Gatherbuddy
{
    public partial class GatherbuddyBot : BotBase
    {
        internal static List<Herb> herbList = new List<Herb>
            {
#if !HB_CN
                new Herb(181166, "Bloodthistle", 1),
                new Herb(1618, "Peacebloom", 1),
                new Herb(1617, "Silverleaf", 1),
                new Herb(1619, "Earthroot", 15),
                new Herb(1620, "Mageroyal", 50),
                new Herb(1621, "Briarthorn", 70),
                new Herb(1622, "Bruiseweed", 85),
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
                new Herb(202751, "Twilight Jasmine", 525),

                new Herb(209349, "Green Tea Leaf", 500), // Start of Panda Herbs
                new Herb(209353, "Rain Poppy", 525),
                new Herb(209350, "Silkweed", 545),
                new Herb(209354, "Golden Lotus", 550),
                new Herb(209351, "Snow Lily", 575),
                new Herb(209355, "Fool's Cap", 600),
                new Herb(214510, "Sha-Touched Herb", 575),
                new Herb(215412, "Sha-Touched Herb (Dread Wastes Specific)", 575),
                // Goggles-based herbs
                new Herb(215405, "Green Tea Leaf (Engineering Goggles)", 500),
                new Herb(215408, "Rain Poppy (Engineering Goggles)", 525),
                new Herb(215406, "Silkweed (Engineering Goggles)", 545),
                // new Herb(209354, "Golden Lotus (Engineering Goggles)", 550), // This one doesn't happen with goggles!
                new Herb(215407, "Snow Lily (Engineering Goggles)", 575),
                new Herb(215410, "Fool's Cap (Engineering Goggles)", 600),
                new Herb(215411, "Sha-Touched Herb (Engineering Goggles)", 575),
                new Herb(215403, "Sha-Touched Herb (Engineering Goggles #2)", 575),

#else
                                                      new Herb(181166, "血蓟", 1),
                                                      new Herb(1618, "宁神花", 1),
                                                      new Herb(1617, "银叶草", 1),
                                                      new Herb(1619, "地根草", 15),
                                                      new Herb(1620, "魔皇草", 50),
                                                      new Herb(1621, "石南草", 70),
                                                      new Herb(1622, "跌打草", 85),
                                                      new Herb(2045, "荆棘藻", 85),
                                                      new Herb(1628, "墓地苔", 105),
                                                      new Herb(1623, "野钢花", 115),
                                                      new Herb(1624, "皇血草", 125),
                                                      new Herb(2042, "枯叶草", 150),
                                                      new Herb(2046, "金棘草", 150),
                                                      new Herb(2041, "活根草", 150),
                                                      new Herb(2043, "卡德加的胡须", 160),
                                                      new Herb(2044, "冬刺草", 195),
                                                      new Herb(2866, "火焰花", 205),
                                                      new Herb(142140, "紫莲花", 210),
                                                      new Herb(142141, "阿尔萨斯之泪", 220),
                                                      new Herb(142142, "太阳草", 230),
                                                      new Herb(142143, "盲目草", 235),
                                                      new Herb(142144, "幽灵菇", 245),
                                                      new Herb(142145, "格罗姆之血", 250),
                                                      new Herb(176583, "黄金参", 260),
                                                      new Herb(176584, "梦叶草", 270),
                                                      new Herb(176588, "冰盖草", 270),
                                                      new Herb(176586, "山鼠草", 280),
                                                      new Herb(176587, "瘟疫花", 285),
                                                      new Herb(176589, "黑莲花", 300),
                                                      new Herb(181270, "魔草", 300),
                                                      new Herb(181271, "梦露花", 315),
                                                      new Herb(181275, "邪雾草", 325),
                                                      new Herb(181277, "泰罗果", 325),
                                                      new Herb(181276, "烈焰菇", 335),
                                                      new Herb(181278, "远古苔", 340),
                                                      new Herb(189973, "金苜蓿", 350),
                                                      new Herb(181279, "虚空花", 350),
                                                      new Herb(185881, "灵尘灌木丛", 350),
                                                      new Herb(191303, "火棘花", 360),
                                                      new Herb(181280, "噩梦藤", 365),
                                                      new Herb(181281, "法力蓟", 375),
                                                      new Herb(190169, "卷丹", 375),
                                                      new Herb(190170, "塔兰德拉的玫瑰", 385),
                                                      new Herb(191019, "蛇信草", 400),
                                                      new Herb(190173, "冰冷的草药", 400),
                                                      new Herb(202749, "艾萨拉雾菇", 425),
                                                      new Herb(202747, "燃烬草", 425),
                                                      new Herb(190171, "巫妖花", 425),
                                                      new Herb(202748, "风暴藤", 425),
                                                      new Herb(190172, "冰棘草", 435),
                                                      new Herb(190176, "雪莲花", 450),
                                                      new Herb(202750, "心灵之花", 475),
                                                      new Herb(202752, "鞭尾草", 500),
                                                      new Herb(202751, "暮光茉莉", 525),
                                                      new Herb(209349, "绿茶叶", 500), // Start of Panda Herbs
                                                      new Herb(209353, "雨罂粟", 525),
                                                      new Herb(209350, "芳草属植物", 545),
                                                      new Herb(209354, "金瓶梅", 550),
                                                      new Herb(209351, "雪百合", 575),
                                                      new Herb(209355, "傻瓜帽", 600),
                                                      new Herb(214510, "沙触摸香草", 575),
                new Herb(215412, "Sha-Touched Herb (Dread Wastes Specific)", 575),
                // Goggles-based herbs
                new Herb(215405, "Green Tea Leaf (Engineering Goggles)", 500),
                new Herb(215408, "Rain Poppy (Engineering Goggles)", 525),
                new Herb(215406, "Silkweed (Engineering Goggles)", 545),
                // new Herb(209354, "Golden Lotus (Engineering Goggles)", 550), // This one doesn't happen with goggles!
                new Herb(215407, "Snow Lily (Engineering Goggles)", 575),
                new Herb(215410, "Fool's Cap (Engineering Goggles)", 600),
                new Herb(215411, "Sha-Touched Herb (Engineering Goggles)", 575),
                new Herb(215403, "Sha-Touched Herb (Engineering Goggles #2)", 575),
#endif
            };

        internal static List<Mineral> mineList = new List<Mineral>
                                                     {
#if !HB_CN
                                                         new Mineral(1731, "Copper Vein", 1),
                                                         new Mineral(191844, "Enchanted Earth", 1),
                                                         new Mineral(191845, "Enchanted Earth", 1),
                                                         new Mineral(188699, "Strange Ore", 1),
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

                                                         new Mineral(209311, "Ghost Iron Deposit", 500), //Start of Panda Mines
                                                         new Mineral(209312, "Kyparite Deposit", 550),
                                                         new Mineral(209328, "Rich Ghost Iron Deposit", 550),
                                                         new Mineral(209329, "Rich Kyparite Deposit", 575),
                                                         new Mineral(209330, "Rich Trillium Vein", 600),
                                                         new Mineral(209313, "Trillium Vein", 600),
                                                         new Mineral(215418, "Trillium Vein (Engineering Goggles)", 600)
#else
                                                         new Mineral(1731, "铜矿", 1),
                                                         new Mineral(191844, "魔化土壤", 1),
                                                         new Mineral(191845, "魔化土壤", 1),
                                                         new Mineral(188699, "奇怪的矿石", 1),
                                                         new Mineral(1610, "火岩矿脉", 65),
                                                         new Mineral(1732, "锡矿", 65),
                                                         new Mineral(2653, "次级血石矿脉", 75),
                                                         new Mineral(73940, "软泥覆盖的银矿脉", 75),
                                                         new Mineral(1733, "银矿", 75),
                                                         new Mineral(1735, "铁矿石", 125),
                                                         new Mineral(19903, "精铁矿脉", 150),
                                                         new Mineral(1734, "金矿石", 155),
                                                         new Mineral(73941, "软泥覆盖的金矿脉", 155),
                                                         new Mineral(2040, "秘银矿脉", 175),
                                                         new Mineral(123310, "软泥覆盖的秘银矿脉", 175),
                                                         new Mineral(123309, "软泥覆盖的真银矿脉", 205),
                                                         new Mineral(2047, "真银矿石", 205),
                                                         new Mineral(165658, "黑铁矿脉", 230),
                                                         new Mineral(123848, "软泥覆盖的瑟银矿脉", 230),
                                                         new Mineral(324, "瑟银矿脉", 230),
                                                         new Mineral(177388, "软泥覆盖的富瑟银矿脉", 255),
                                                         new Mineral(175404, "富瑟银矿", 255),
                                                         new Mineral(181555, "魔铁矿脉", 275),
                                                         new Mineral(185877, "虚空矿脉", 275),
                                                         new Mineral(181069, "大型黑曜石碎块", 305),
                                                         new Mineral(181068, "小型黑曜石碎块", 305),
                                                         new Mineral(181556, "精金矿脉", 325),
                                                         new Mineral(189978, "钴矿脉", 350),
                                                         new Mineral(181569, "富精金矿脉", 350),
                                                         new Mineral(185557, "上古宝石矿脉", 375),
                                                         new Mineral(181557, "氪金矿脉", 375),
                                                         new Mineral(189979, "富钴矿脉", 375),
                                                         new Mineral(189980, "蓝铜矿脉", 400),
                                                         new Mineral(202736, "黑曜石碎块", 425),
                                                         new Mineral(189981, "富萨隆邪铁矿脉", 425),
                                                         new Mineral(195036, "纯净的萨隆邪铁矿脉", 450),
                                                         new Mineral(202739, "巨型黑曜石石板", 450),
                                                         new Mineral(191133, "泰坦神铁矿脉", 450),
                                                         new Mineral(202738, "源质矿", 475),
                                                         new Mineral(202741, "富源质矿", 500),
                                                         new Mineral(202737, "燃铁矿脉", 525),
                                                         new Mineral(202740, "豐沃的黃鐵礦床", 525),
                                                         new Mineral(209311, "鬼铁矿床", 500), //Start of Panda Mines
                                                         new Mineral(209312, "存款", 550),
                                                         new Mineral(209328, "丰富的鬼铁沉积t", 550),
                                                         new Mineral(209329, "丰富的存款", 575),
                                                         new Mineral(209330, "丰富的延龄草静脉", 600),
                                                         new Mineral(209313, "延龄草静脉", 600),
                                                         new Mineral(215418, "Trillium Vein (Engineering Goggles)", 600)
#endif
                                                     };

        internal static List<uint> nodeAvoidList = new List<uint>
                                                       {
                                                           185881,
                                                           //Netherdust Bush
                                                           190702,
                                                           //Lifeblood Shard
                                                           185877,
                                                           //Nethercite Deposite  
                                                           1610,
                                                           //Incendicite Mineral Vein  
                                                           19903,
                                                           //Indurium Mineral Vein
                                                           73940,
                                                           // Ooze nodes
                                                           73941,
                                                           123310,
                                                           123309,
                                                           123848,
                                                           177388,
                                                           //
                                                       };

        #region Nested type: Herb

        internal class Herb
        {
            private readonly uint entry;
            private readonly string name;
            private readonly uint requiredSkill;

            public Herb(uint Entry, string Name, uint RequiredSkill)
            {
                entry = Entry;
                name = Name;
                requiredSkill = RequiredSkill;
            }

            public uint Entry
            {
                get { return entry; }
            }

            public string Name
            {
                get { return name; }
            }

            public uint RequiredSkill
            {
                get { return requiredSkill; }
            }
        }

        #endregion

        #region Nested type: Mineral

        internal class Mineral
        {
            private readonly uint entry;
            private readonly string name;
            private readonly uint requiredSkill;

            public Mineral(uint Entry, string Name, uint RequiredSkill)
            {
                entry = Entry;
                name = Name;
                requiredSkill = RequiredSkill;
            }

            public uint Entry
            {
                get { return entry; }
            }

            public string Name
            {
                get { return name; }
            }

            public uint RequiredSkill
            {
                get { return requiredSkill; }
            }
        }

        #endregion
    }
}