using System;
using System.Collections.Generic;
using System.Linq;

namespace Styx.WoWInternals
{
    public static class Battlegrounds
    {
        static Battlegrounds()
        {
            Styx.CommonBot.BotEvents.Battleground.OnBattlegroundLeft += Battleground_OnBattlegroundLeft;
        }

        private static void Battleground_OnBattlegroundLeft(EventArgs args)
        {
            WoWMovement.MoveStop();
        }

        public static TimeSpan BattlefieldInstanceRunTime
        {
            get { return DateTime.Now - BattlefieldStartTime; }
        }

        public static DateTime BattlefieldStartTime
        {
            get { return StyxWoW.WoWClient.PerformanceCounterToDateTime((ulong)StyxWoW.Memory.Read<uint>(StyxWoW.Offsets.Statics.BattlefieldInstanceStartTime)); }
        }

        public static Styx.CommonBot.Landmarks LandMarks
        {
            get { return _landmarks; }
        }

        public static BattlegroundType Current
        {
            get { return GetCurrentBattleground(); }
        }

        public static bool Finished
        {
            get { return StyxWoW.Memory.Read<uint>(StyxWoW.Offsets.Statics.BattlefieldEnded) != 0U; }
        }

        public static BattlefieldWinner Winner
        {
            get { return GetBattlefieldWinner(); }
        }

        public static List<BattlegroundStatus> BattlegroundStatuses
        {
            get
            {
                List<BattlegroundStatus> list = new List<BattlegroundStatus>();
                foreach (Styx.Patchables.QueuedBattlegroundInfo info in QueuedInfos)
                {
                    list.Add(info.Status);
                }

                return list;
            }
        }

        public static bool IsInsideBattleground
        {
            get { return GetBGIndexWithStatus(BattlegroundStatus.Active) != -1; }
        }

        public static string GetProfileName(BattlegroundType type)
        {
            if (type <= BattlegroundType.IoC)
            {
                switch (type)
                {
                    case BattlegroundType.AV:
                        return "AV.xml";
                    case BattlegroundType.WSG:
                        return "WSG.xml";
                    case BattlegroundType.AB:
                        return "AB.xml";
                    case (BattlegroundType)4:
                    case (BattlegroundType)5:
                    case (BattlegroundType)6:
                    case (BattlegroundType)8:
                        break;
                    case BattlegroundType.EotS:
                        return "EotS.xml";
                    case BattlegroundType.SotA:
                        return "SotA.xml";
                    default:
                        if (type == BattlegroundType.IoC)
                        {
                            return "IoC.xml";
                        }
                        break;
                }
            }
            else
            {
                if (type == BattlegroundType.TP)
                {
                    return "TP.xml";
                }

                if (type == BattlegroundType.BfG)
                {
                    return "BfG.xml";
                }
            }

            return null;
        }

        public static bool WaitingForConfirmation
        {
            get { return GetBGIndexWithStatus(BattlegroundStatus.Confirm) != -1; }
        }

        public static bool IsQueuedForBattleground(BattlegroundType type)
        {
            return QueuedInfos.Any(s => s.Type == type && s.Status != BattlegroundStatus.None);
        }

        public static void AcceptBattlegroundConfirmation()
        {
            int index = GetBGIndexWithStatus(BattlegroundStatus.Confirm);
            if (index != -1)
            {
                AcceptBattlefieldPort(index, true);
            }
        }

        public static void AcceptBattlefieldPort(int index, bool accept)
        {
            Lua.DoString("AcceptBattlefieldPort({0},{1})", index, accept ? 1 : 0);
        }

        public static int GetBGIndexWithStatus(BattlegroundStatus status)
        {
            foreach (Styx.Patchables.QueuedBattlegroundInfo info in QueuedInfos)
            {
                if (info.Status == status)
                {
                    return (int)info.BattlefieldIndex;
                }
            }

            return -1;
        }

        public static BattlegroundStatus GetStatus(uint index)
        {
            if (index >= 2U)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            return GetQueuedBattlegroundInfo(index).Status;
        }

        public static BattlegroundType GetCurrentBattleground()
        {
            uint mapId = StyxWoW.Me.MapId;
            uint num = mapId;
            if (num <= 607U)
            {
                if (num <= 489U)
                {
                    if (num == 30U)
                    {
                        return BattlegroundType.AV;
                    }

                    if (num == 489U)
                    {
                        return BattlegroundType.WSG;
                    }
                }
                else
                {
                    if (num == 529U)
                    {
                        return BattlegroundType.AB;
                    }

                    if (num == 566U)
                    {
                        return BattlegroundType.EotS;
                    }

                    if (num == 607U)
                    {
                        return BattlegroundType.SotA;
                    }
                }
            }
            else if (num <= 727U)
            {
                if (num == 628U)
                {
                    return BattlegroundType.IoC;
                }

                switch (num)
                {
                    case 726U:
                        return BattlegroundType.TP;
                    case 727U:
                        return BattlegroundType.SM;
                }
            }
            else
            {
                if (num == 761U)
                {
                    return BattlegroundType.BfG;
                }

                if (num == 998U)
                {
                    return BattlegroundType.ToK;
                }

                if (num == 1105U)
                {
                    return BattlegroundType.DG;
                }
            }

            return BattlegroundType.None;
        }

        internal static IEnumerable<Styx.Patchables.QueuedBattlegroundInfo> QueuedInfos
        {
            get
            {
                IntPtr ptr = StyxWoW.Memory.Read<IntPtr>(StyxWoW.Offsets.Statics.CurrentQueuedBGs);
                if (((int)ptr & 1) == 0 && ptr != IntPtr.Zero)
                {
                    while (((int)ptr & 1) == 0 && ptr != IntPtr.Zero)
                    {
                        Styx.Patchables.QueuedBattlegroundInfo info = StyxWoW.Memory.Read<Styx.Patchables.QueuedBattlegroundInfo>(ptr);
                        yield return info;
                        ptr = info.NextBgInfo;
                    }
                }

                yield break;
            }
        }

        public static TimeSpan GetQueuedBattlegroundWaitTime(uint index)
        {
            Styx.Patchables.QueuedBattlegroundInfo info = GetQueuedBattlegroundInfo(index);
            if (info.Status != BattlegroundStatus.Queued)
            {
                return TimeSpan.Zero;
            }

            return TimeSpan.FromMilliseconds(StyxWoW.WoWClient.PerformanceCounter() - (ulong)info.TimeWaited);
        }

        public static Styx.Patchables.QueuedBattlegroundInfo GetQueuedBattlegroundInfo(uint index)
        {
            if (index >= 2U)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            return QueuedInfos.FirstOrDefault(q => q.BattlefieldIndex == index);
        }

        public static BattlefieldWinner GetBattlefieldWinner()
        {
            return (BattlefieldWinner)StyxWoW.Memory.Read<uint>(StyxWoW.Offsets.Statics.BattlefieldWinner);
        }

        public static void JoinBattlefield(BattlegroundType type, bool asGroup = false)
        {
            if (type == BattlegroundType.None)
            {
                return;
            }

            Lua.DoString(string.Format("JoinBattlefield({0}, {1})", (uint)type, asGroup ? "true" : "false"), "WoW.lua");
            StyxWoW.Sleep(500);
        }

        public static void LeaveBattlefield()
        {
            Lua.DoString("LeaveBattlefield()", "WoW.lua");
        }

        private static readonly Styx.CommonBot.Landmarks _landmarks = new Styx.CommonBot.Landmarks();
    }
}
