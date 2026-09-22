using System;
using System.Runtime.InteropServices;
using Styx.WoWInternals.WoWObjects;

namespace Styx.WoWInternals.Misc
{
    /// <summary>
    /// Represents an item on the auction house.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 216)]
    public struct WoWAuction
    {
        private readonly uint _dword0;
        public uint AuctionId;
        public uint ItemId;
        private readonly uint _dwordC;
        private readonly uint _dword10;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 108)]
        private readonly byte[] _padding1;
        public uint SuffixId;
        private readonly uint _dword84;
        private readonly uint _dword88;
        public int StackCount;
        private readonly uint _isSold;
        private readonly uint _unk4;
        public ulong PosterGuid;
        public long MinBid;
        public long MinIncrement;
        public long BuyoutPrice;
        public readonly uint EndTime;
        private readonly uint _dwordBC;
        public ulong CurrentBidderGuid;
        public long CurrentBid;
        private readonly uint _dwordD0;
        private readonly uint _dwordD4;

        public bool IsSold
        {
            get { return _isSold != 0U; }
        }

        public TimeSpan TimeLeft
        {
            get
            {
                ulong currentTime = StyxWoW.WoWClient.PerformanceCounter();
                ulong timeLeft = EndTime - currentTime;
                return TimeSpan.FromMilliseconds(timeLeft);
            }
        }

        public DateTime ExpireTime
        {
            get { return DateTime.Now.Add(TimeLeft); }
        }

        public ItemInfo ItemInfo
        {
            get { return ItemInfo.FromId(ItemId); }
        }

        public bool CanUse
        {
            get
            {
                GameError error;
                return StyxWoW.Me.CanUseItem(ItemId, out error);
            }
        }

        public string ItemName
        {
            get { return ItemInfo != null ? ItemInfo.Name : "Unknown"; }
        }

        public override string ToString()
        {
            return string.Format(
                "[{0}] AuctionId:{1} MinBid:{2} CurrentBid:{3} MinIncrement:{4} BuyoutPrice:{5} StackCount:{6} ItemId:{7} CanUse:{8} IsSold:{9} TimeLeft:{10}",
                ItemName, AuctionId, MinBid, CurrentBid, MinIncrement, BuyoutPrice, StackCount, ItemId,
                CanUse, IsSold, TimeLeft);
        }
    }
}
