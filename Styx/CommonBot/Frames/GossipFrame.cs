using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Styx.WoWInternals;

namespace Styx.CommonBot.Frames
{
    public class GossipFrame : Frame
    {
        public static readonly GossipFrame Instance = new GossipFrame();

        public GossipFrame()
            : base("GossipFrame")
        {
        }

        public void Close()
        {
            Lua.DoString("CloseGossip() GossipFrameCloseButton:Click()", "WoW.lua");
        }

        public List<GossipQuestEntry> ActiveQuests
        {
            get
            {
                var quests = new List<GossipQuestEntry>();
                if (IsVisible)
                {
                    for (int i = 0; i < 40; i++)
                    {
                        try
                        {
                            var quest = GetActiveQuestInfoFromIndex(i);
                            if (quest != null)
                                quests.Add(quest);
                        }
                        catch
                        {
                            break;
                        }
                    }

                    return quests;
                }

                return quests;
            }
        }

        public List<GossipQuestEntry> AvailableQuests
        {
            get
            {
                var quests = new List<GossipQuestEntry>();

                using (StyxWoW.Memory.AcquireFrame())
                {
                    if (IsVisible)
                    {
                        for (int i = 0; i < 40; i++)
                        {
                            try
                            {
                                var quest = GetAvaibleQuestInfoFromIndex(i);
                                if (quest != null)
                                    quests.Add(quest);
                            }
                            catch
                            {
                                break;
                            }
                        }
                    }
                }

                return quests;
            }
        }

        public List<GossipEntry> GossipOptionEntries
        {
            get
            {
                List<string> gossips = Lua.GetReturnValues("return GetGossipOptions()", "_main.lua");

                if (gossips == null)
                    return null;

                return ParseGossipOptionsInfo(gossips.ToArray());
            }
        }

        public string GossipText { get { return Lua.GetReturnVal<string>("return GetGossipText()", 0); } }

        public void SelectActiveGossipQuest(int index)
        {
            Lua.DoString(string.Format("SelectGossipActiveQuest({0})", index + 1), "WoW.lua");
        }

        public void SelectActiveQuest(int index)
        {
            Lua.DoString(string.Format("SelectActiveQuest({0})", index + 1), "WoW.lua");
        }

        public void SelectGossipOption(int index)
        {
            Lua.DoString(string.Format("SelectGossipOption({0})", index + 1), "_main.lua");
        }

        public void SelectAvailableQuest(int index)
        {
            Lua.DoString(string.Format("SelectAvailableQuest({0}) SelectGossipAvailableQuest({0})", index + 1), "_main.lua");
        }

        public new void Hide()
        {
            Close();
        }

        private static List<GossipEntry> ParseGossipOptionsInfo(string[] gossips)
        {
            if (gossips == null || gossips.Length < 2)
            {
                return null;
            }

            var gossip = new List<GossipEntry>();
            for (int i = 0; i < gossips.Length / 2; i++)
            {
                GossipEntry.GossipEntryType type;

                try
                {
                    type = (GossipEntry.GossipEntryType)Enum.Parse(typeof(GossipEntry.GossipEntryType), gossips[i * 2 + 1], true);
                }
                catch (InvalidCastException)
                {
                    type = GossipEntry.GossipEntryType.Unknown;
                }
                catch (ArgumentException)
                {
                    type = GossipEntry.GossipEntryType.Unknown;
                }

                gossip.Add(new GossipEntry { Text = gossips[i * 2], Type = type, Index = i });
            }

            return gossip;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct UnkTemp
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
            public GossipQuestEntry.QuestEntry2[] Entries;
        }

        internal static GossipQuestEntry GetAvaibleQuestInfoFromIndex(int index)
        {
            if (QuestFrame.Instance.IsVisible)
            {
                var natives = StyxWoW.Memory.ReadArray<GossipQuestEntry.QuestEntry>(StyxWoW.Offsets.Statics.GossipAvaibleQuestsArray, 40);
                var entry = new GossipQuestEntry(natives[index], index);
                if (entry.IsValid)
                    return entry;
            }

            var newNatives = StyxWoW.Memory.ReadArray<UnkTemp>(StyxWoW.Offsets.Statics.GossipQuestsForObjectsArray, 8);
            var entries = new List<GossipQuestEntry>();
            int idx = 0;
            foreach (var newNative in newNatives)
            {
                entries.AddRange(from q in newNative.Entries
                                  where q.QuestId != 0 && q.Status != 3 && q.Status != 4
                                  select new GossipQuestEntry(q, idx++));
            }

            if (entries.Count > index)
                return entries[index];

            return null;
        }

        internal static GossipQuestEntry GetActiveQuestInfoFromIndex(int index)
        {
            if (QuestFrame.Instance.IsVisible)
            {
                var natives = StyxWoW.Memory.ReadArray<GossipQuestEntry.QuestEntry>(StyxWoW.Offsets.Statics.GossipActiveQuestsArray, 40);
                var entry = new GossipQuestEntry(natives[index], index);
                if (entry.IsValid)
                    return entry;
            }

            var newNatives = StyxWoW.Memory.ReadArray<UnkTemp>(StyxWoW.Offsets.Statics.GossipQuestsForObjectsArray, 8);
            var entries = new List<GossipQuestEntry>();
            int idx = 0;
            foreach (var newNative in newNatives)
            {
                entries.AddRange(from q in newNative.Entries
                                  where q.QuestId != 0 && (q.Status == 3 || q.Status == 4)
                                  select new GossipQuestEntry(q, idx++));
            }

            if (entries.Count > index)
                return entries[index];

            return null;
        }
    }
}
