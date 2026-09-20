using System;
using System.Runtime.InteropServices;
using Styx.Helpers;
using Styx.WoWInternals.WoWCache;

namespace Styx.CommonBot.Frames
{
    public class GossipQuestEntry
    {
        private readonly QuestEntry _questEntry;

        internal GossipQuestEntry(QuestEntry entry, int index)
        {
            Index = index;
            _questEntry = entry;
        }

        internal GossipQuestEntry(QuestEntry2 entry, int index)
        {
            Index = index;
            _questEntry = new QuestEntry();
            _questEntry.Id = entry.QuestId;
            _questEntry.Name = entry.Name;
            _questEntry.Flags = (WoWCache.QuestFlags)entry.Flags;
            _questEntry.MoreFlags_Repeatable = entry.MoreFlags;
            _questEntry.IsCompletedQuest = entry.IsCompleted;
            _questEntry.RequiredLevel = entry.RequiredLevel;
        }

        public bool IsValid { get { return Id != 0; } }
        public int Id { get { return _questEntry.Id; } }

        public string Name
        {
            get { return _questEntry.Name.ToUTF8String(); }
        }

        public int RequiredLevel { get { return _questEntry.RequiredLevel; } }

        public int Index;

        #region Nested Type: QuestEntry

        [StructLayout(LayoutKind.Sequential)]
        internal struct QuestEntry
        {
            public int Id;
            public int RequiredLevel;
            public WoWCache.QuestFlags Flags;
            public int MoreFlags_Repeatable;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)]
            public byte[] Name;
            public int IsRepetable;
            public int IsCompletedQuest;

            public override string ToString()
            {
                return string.Format("Id: {0}, RequiredLevel: {1}, Flags: {2}, MoreFlagsRepeatable: {3}, Name: {4}, IsRepetable: {5}, IsCompletedQuest: {6}", Id, RequiredLevel, Flags, MoreFlags_Repeatable, Name.ToUTF8String(), IsRepetable, IsCompletedQuest);
            }
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct QuestEntry2
        {
            public int QuestId;
            public int RequiredLevel;
            public int Flags;
            public int MoreFlags;
            public int IsCompleted;
            public int Status;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)]
            public byte[] Name;

            public override string ToString()
            {
                return string.Format("QuestId: {0}, Status: {1}, Name: {2}", QuestId, Status, Name.ToUTF8String());
            }
        }
        #endregion

        public override string ToString()
        {
            if (!IsValid)
                return "Invalid Quest Entry";

            return string.Format("Id: {0}, Name: {1}, RequiredLevel: {2}, Index: {3}", Id, Name, RequiredLevel, Index);
        }
    }
}
