using System;
using Styx.Offsets;

namespace Styx.WoWInternals.WoWObjects
{
    public class WoWCorpse : WoWObject
    {
        public WoWCorpse(IntPtr baseAdress)
            : base(baseAdress)
        {
        }

        public uint DisplayId
        {
            get { return GetStorageField<uint>((uint)WoWCorpseFields.DisplayID); }
        }

        public ulong Owner
        {
            get { return GetStorageField<ulong>((uint)WoWCorpseFields.Owner); }
        }

        public ulong Party
        {
            get { return GetStorageField<ulong>((uint)WoWCorpseFields.PartyGUID); }
        }

        public new uint DynamicFlags
        {
            get { return GetStorageField<uint>((uint)WoWCorpseFields.DynamicFlags); }
        }

        private uint Flags
        {
            get { return GetStorageField<uint>((uint)WoWCorpseFields.Flags); }
        }

        public bool Lootable
        {
            get { return HasFlag(Flags, 32U); }
        }

        public bool IsOnlyBones
        {
            get { return HasFlag(Flags, 1U); }
        }

        private enum CorpseFlags
        {
            None,
            Bones,
            Unk1,
            Unk2 = 4,
            HideHelm = 8,
            HideCloak = 16,
            Lootable = 32
        }
    }
}
