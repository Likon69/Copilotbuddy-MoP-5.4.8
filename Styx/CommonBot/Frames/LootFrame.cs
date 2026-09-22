using System;
using System.Runtime.InteropServices;
using Styx.Common;
using Styx.WoWInternals;

namespace Styx.CommonBot.Frames
{
    public class LootFrame : Frame
    {
        public LootFrame()
            : base("LootFrame")
        {
        }

        public new bool IsVisible
        {
            get
            {
                return this.LootingObjectGuid != 0UL;
            }
        }

        public ulong LootingObjectGuid
        {
            get
            {
                return StyxWoW.Memory.Read<ulong>(StyxWoW.Offsets.Statics.LootFrame - 16);
            }
        }

        public LootSlotInfo LootInfo(int slot)
        {
            return new LootSlotInfo(slot);
        }

        public uint GetItemId(int slot)
        {
            IntPtr lootFrameAddress = StyxWoW.Offsets.Statics.LootFrame;
            IntPtr lootSlotInfoAddress = StyxWoW.CallInjected<IntPtr>(StyxWoW.Offsets.Functions.CGLootInfo__GetLootSlotInfo, CallingConvention.ThisCall, new object[]
            {
                (uint)(int)lootFrameAddress,
                slot
            });
            if (lootSlotInfoAddress == (IntPtr)0)
            {
                return 0U;
            }
            LootFrame.NativeLootSlotInfo nativeLootSlotInfo = StyxWoW.Memory.Read<LootFrame.NativeLootSlotInfo>(lootSlotInfoAddress);
            Styx.Common.Logging.WriteDiagnostic(nativeLootSlotInfo.ToString());
            return nativeLootSlotInfo.ItemId;
        }

        public int LootItems
        {
            get
            {
                return Styx.WoWInternals.Lua.GetReturnVal<int>("return GetNumLootItems()", 0U);
            }
        }

        public void Loot(int slot)
        {
            Styx.WoWInternals.Lua.DoString("LootSlot({0})", new object[] { slot + 1 });
        }

        public void LootAll()
        {
            Styx.WoWInternals.Lua.DoString("for i=1, GetNumLootItems() do LootSlot(i) ConfirmBindOnUse() end CloseLoot()", "WoW.lua");
        }

        public void Close()
        {
            if (this.IsVisible)
            {
                Styx.WoWInternals.Lua.DoString("CloseLoot()", "WoW.lua");
            }
        }

        public static readonly LootFrame Instance = new LootFrame();

        public struct NativeLootSlotInfo
        {
            public override string ToString()
            {
                return string.Format("ItemId: {0}, IsCurrency: {1}, IsCoinage: {2}, Byte6: {3}, Byte7: {4}, TextureId: {5}, Quantity: {6}, Int10: {7}, Int14: {8}, Status: {9}", new object[] { this.ItemId, this.IsCurrency, this.IsCoinage, this.byte6, this.byte7, this.TextureId, this.Quantity, this.int10, this.int14, this.Status });
            }

            public uint ItemId;

            public byte IsCurrency;

            public byte IsCoinage;

            public byte byte6;

            public byte byte7;

            public int TextureId;

            public int Quantity;

            public int int10;

            public int int14;

            public int Status;
        }
    }
}
