using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GreenMagic;
using Styx.WoWInternals;
using Styx.WoWInternals.Misc;
using Styx.WoWInternals.WoWObjects;

namespace Styx.CommonBot.Frames
{
    public class AuctionFrame : Frame
    {
        public static readonly AuctionFrame Instance = new AuctionFrame();

        private static int searchPage;

        public bool CanPerformSearch => Lua.GetReturnVal<bool>("return CanSendAuctionQuery()", 0u);

        public bool CanPerformGetAllSearch => Lua.GetReturnVal<bool>("return CanSendAuctionQuery()", 1u);

        public int NumListAuctions
        {
            get
            {
                GetNumAuctionItems(AuctionListType.List, out var batch, out var _);
                return batch;
            }
        }

        public int NumBidderAuctions
        {
            get
            {
                GetNumAuctionItems(AuctionListType.Bidder, out var batch, out var _);
                return batch;
            }
        }

        public int NumOwnerAuctions
        {
            get
            {
                GetNumAuctionItems(AuctionListType.Owner, out var batch, out var _);
                return batch;
            }
        }

        public int FullNumBidderAuctions
        {
            get
            {
                GetNumAuctionItems(AuctionListType.Bidder, out var _, out var totalCount);
                return totalCount;
            }
        }

        public int FullNumListAuctions
        {
            get
            {
                GetNumAuctionItems(AuctionListType.List, out var _, out var totalCount);
                return totalCount;
            }
        }

        public int NumListAuctionPages
        {
            get
            {
                int fullNumListAuctions = FullNumListAuctions;
                if (fullNumListAuctions <= 50)
                {
                    return 1;
                }
                return (int)Math.Ceiling((float)fullNumListAuctions / 50f);
            }
        }

        public AuctionFrame()
            : base("AuctionFrame")
        {
        }

        public void Close()
        {
            Lua.DoString("CloseAuctionHouse()");
        }

        public override void Hide()
        {
            Close();
        }

        public ulong CalculateAuctionDeposit(int runTime, int stackSize, int numStacks)
        {
            return Lua.GetReturnVal<ulong>($"return CalculateAuctionDeposit({runTime}, {stackSize}, {numStacks})", 0u);
        }

        public bool CanCancelAuction(int index)
        {
            return Lua.GetReturnVal<bool>($"return CanCancelAuction({index})", 0u);
        }

        public void CancelSell()
        {
            Lua.DoString("CancelSell()");
        }

        public void CancelAuction(int index)
        {
            Lua.DoString("CancelAuction({0})", index);
        }

        public void ClickAuctionSellItemButton()
        {
            Lua.DoString("ClickAuctionSellItemButton()");
        }

        public ulong GetAuctionHouseDepositRate()
        {
            return Lua.GetReturnVal<ulong>("return GetAuctionHouseDepositRate()", 0u);
        }

        public void GetNumAuctionItems(AuctionListType type, out int batch, out int totalCount)
        {
            switch (type)
            {
            default:
                throw new ArgumentException("type");
            case AuctionListType.List:
                batch = StyxWoW.Memory.Read<int>(StyxWoW.Offsets.Statics.NumListAuctions);
                totalCount = StyxWoW.Memory.Read<int>(StyxWoW.Offsets.Statics.NumTotalListAuctions);
                break;
            case AuctionListType.Bidder:
                batch = StyxWoW.Memory.Read<int>(StyxWoW.Offsets.Statics.NumBidderAuctions);
                totalCount = StyxWoW.Memory.Read<int>(StyxWoW.Offsets.Statics.NumTotalBidAuctions);
                break;
            case AuctionListType.Owner:
                batch = StyxWoW.Memory.Read<int>(StyxWoW.Offsets.Statics.NumOwnerAuctions);
                totalCount = batch;
                break;
            }
        }

        public bool GetAuction(AuctionListType list, int index, out WoWAuction auction)
        {
            GetNumAuctionItems(list, out var batch, out var _);
            if (index >= 0 && index < batch)
            {
                ReadAuction(list, index, out auction);
                return true;
            }
            auction = default(WoWAuction);
            return false;
        }

        public WoWAuction[] GetAllBatchAuctions(AuctionListType list)
        {
            GetNumAuctionItems(list, out var batch, out var _);
            if (batch <= 0)
            {
                return new WoWAuction[0];
            }
            IntPtr address = GetAuctionListAddress(list);
            return StyxWoW.Memory.ReadArray<WoWAuction>(StyxWoW.Memory.Read<IntPtr>(address), batch);
        }

        public WoWAuction[] GetOwnedAuctions()
        {
            return GetAllBatchAuctions(AuctionListType.Owner);
        }

        public WoWAuction[] GetBidderAuctions()
        {
            return GetAllBatchAuctions(AuctionListType.Bidder);
        }

        public WoWAuction[] GetListAuctions()
        {
            return GetAllBatchAuctions(AuctionListType.List);
        }

        [Obsolete("This API is outdated and should not be used. Consider using Lua.")]
        public List<WoWAuction> PerformSearch(string itemName)
        {
            return PerformSearch(itemName, 0);
        }

        [Obsolete("This API is outdated and should not be used. Consider using Lua.")]
        public List<WoWAuction> PerformSearch(string itemName, int page)
        {
            if (!base.IsVisible)
            {
                return null;
            }
            using (StyxWoW.Memory.ReleaseFrame(reacquireAsHardLock: true))
            {
                while (!CanPerformSearch)
                {
                    StyxWoW.Sleep(250);
                }
            }
            Stopwatch stopwatch = new Stopwatch();
            int fullNumListAuctions = FullNumListAuctions;
            stopwatch.Start();
            Lua.DoString("QueryAuctionItems(\"{0}\", 0, 0, 0, 0, 0, {1}, 0, 0, 0)", itemName, page);
            using (StyxWoW.Memory.ReleaseFrame(reacquireAsHardLock: true))
            {
                while (FullNumListAuctions == fullNumListAuctions && !CanPerformSearch && stopwatch.ElapsedMilliseconds < 10000L)
                {
                    StyxWoW.Sleep(250);
                }
            }
            stopwatch.Stop();
            List<WoWAuction> auctions = GetListAuctions().ToList();
            int numListAuctionPages = NumListAuctionPages;
            searchPage++;
            if (numListAuctionPages > 1 && searchPage < numListAuctionPages)
            {
                auctions.AddRange(PerformSearch(itemName, searchPage));
            }
            return auctions;
        }

        private IntPtr GetAuctionListAddress(AuctionListType list)
        {
            return list switch
            {
                AuctionListType.List => StyxWoW.Offsets.Statics.ListAuctions,
                AuctionListType.Bidder => StyxWoW.Offsets.Statics.BidderAuctions,
                AuctionListType.Owner => StyxWoW.Offsets.Statics.OwnerAuctions,
                _ => throw new ArgumentException("list"),
            };
        }

        internal void ReadAuction(AuctionListType list, int index, out WoWAuction auction)
        {
            switch (list)
            {
            default:
                throw new ArgumentException("list");
            case AuctionListType.List:
            case AuctionListType.Bidder:
            case AuctionListType.Owner:
            {
                IntPtr address = GetAuctionListAddress(list);
                auction = StyxWoW.Memory.Read<WoWAuction>(isRelative: false, new IntPtr[2]
                {
                    address,
                    (IntPtr)(index * MarshalCache<WoWAuction>.Size)
                });
                break;
            }
            }
        }

        [Obsolete("This API is outdated and should not be used. Consider using Lua.")]
        public void PostAuction(long minBid, long buyout, AuctionPostTime postTime, WoWItem item, uint stackSize, uint numStacks)
        {
            if (base.IsVisible)
            {
                if (item == null)
                {
                    throw new ArgumentNullException("item");
                }
                item.PickUp();
                Lua.DoString("ClickAuctionSellItemButton()");
                Lua.DoString("ClearCursor()");
                StyxWoW.Sleep(1000);
                Lua.DoString("StartAuction({0}, {1}, {2}, {3}, {4})", minBid, buyout, (int)postTime, stackSize, numStacks);
            }
        }
    }
}
