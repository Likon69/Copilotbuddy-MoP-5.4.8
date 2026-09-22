using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Media;
using Styx;
using Styx.Common;
using Styx.CommonBot.CharacterManagement;
using Styx.CommonBot.Frames;
using Styx.Helpers;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace Levelbot.Actions.General
{
    public class ActionSelectReward : Styx.TreeSharp.Action
    {
        protected override RunStatus Run(object context)
        {
            using (StyxWoW.Memory.AcquireFrame())
            {
                if (!QuestFrame.Instance.IsVisible)
                    return RunStatus.Failure;

                Frame frame = new Frame("QuestInfoItem1");
                if (!frame.IsVisible)
                    return RunStatus.Success;

                List<string> returnValues = Lua.GetReturnValues("local t = {} for i=1,GetNumQuestChoices() do table.insert(t, GetQuestItemLink(\"choice\", i)) table.insert(t, (select(3, GetQuestItemInfo(\"choice\", i)))) end return unpack(t)");
                if (returnValues == null || returnValues.Count <= 0)
                    return RunStatus.Success;

                QuestRewardItem[] rewards = new QuestRewardItem[returnValues.Count / 2];
                for (int i = 0; i < returnValues.Count / 2; i++)
                {
                    string link = returnValues[i * 2];
                    ItemInfo itemInfo;
                    try
                    {
                        itemInfo = ItemInfo.FromLink(link);
                    }
                    catch (ArgumentException)
                    {
                        Logging.Write("Got invalid item link in ActionSelectReward: {0}", link);
                        continue;
                    }

                    if (itemInfo == null)
                    {
                        Logging.Write("Could not retrieve item info in ActionSelectReward for item link {0}", link);
                        continue;
                    }

                    string countText = returnValues[i * 2 + 1];
                    int count;
                    if (!int.TryParse(countText, NumberStyles.Number, CultureInfo.InvariantCulture, out count))
                    {
                        Logging.Write("Could not parse count for {0} in ActionSelectReward! Got: {1}", link, countText);
                        continue;
                    }

                    rewards[i] = new QuestRewardItem
                    {
                        Index = i,
                        ItemLink = link,
                        ItemInfo = itemInfo,
                        Count = count
                    };
                }

                if (rewards.Length <= 1)
                    return RunStatus.Success;

                QuestRewardItem best = null;
                if (CharacterSettings.Instance.AutoEquip)
                {
                    AutoEquipper autoEquip = CharacterManager.AutoEquip;
                    best = autoEquip.GetBestQuestReward(rewards.Where(r => r != null).ToArray());
                }

                if (best == null)
                {
                    int bestValue = int.MinValue;
                    foreach (QuestRewardItem reward in rewards)
                    {
                        if (reward != null)
                        {
                            int sellValue = reward.ItemInfo.SellPrice * reward.Count;
                            Logging.Write("{0}{1} sells for {2}", reward.ItemInfo.Name, reward.Count > 1 ? "x" + reward.Count : "", sellValue);
                            if (sellValue > bestValue)
                            {
                                bestValue = sellValue;
                                best = reward;
                            }
                        }
                    }

                    if (best != null)
                        Logging.Write(Colors.LawnGreen, "Selecting {0} as the quest reward as it sells for the most ({1} copper)", best.ItemInfo.Name, bestValue);
                }

                int rewardIndex;
                if (best == null)
                {
                    rewardIndex = 0;
                    Logging.Write(Colors.LawnGreen, "Selecting first reward as we could not evaluate any quest rewards properly");
                }
                else
                {
                    rewardIndex = best.Index;
                }

                QuestFrame.Instance.SelectQuestReward(rewardIndex);
                return RunStatus.Success;
            }
        }
    }
}
