using System;
using GreenMagic;
using Styx.Common;
using Styx.Patchables;
using Styx.Pathing;
using Styx.WoWInternals.DBC;

namespace Styx.WoWInternals
{
    public class AreaPoiLandmark : WoWLandMark
    {
        public AreaPoiLandmark(int index, LandMarkEntry data)
            : base(index, data)
        {
            areaPoi_0 = AreaPoi.FromId(Entry);
        }

        public override bool IsValid
        {
            get { return base.IsValid && areaPoi_0 != null; }
        }

        public override string Name
        {
            get { return areaPoi_0.Name; }
        }

        public override string Description
        {
            get { return areaPoi_0.Description; }
        }

        public override int TextureIndex
        {
            get
            {
                uint num = WorldState - 1U;
                if ((ulong)num >= (ulong)(long)areaPoi_0.Icons.Length)
                {
                    num = 0U;
                }
                return areaPoi_0.Icons[(int)((UIntPtr)num)];
            }
        }

        public override WoWPoint Location
        {
            get
            {
                if (woWPoint_0 != WoWPoint.Empty)
                {
                    return woWPoint_0;
                }

                Vector2 location = areaPoi_0.Location;
                if (location == Vector2.Zero)
                {
                    return WoWPoint.Empty;
                }

                float num;
                if (Styx.Pathing.Navigator.FindHeight(location.X, location.Y, out num))
                {
                    woWPoint_0 = new WoWPoint(location.X, location.Y, num);
                    return woWPoint_0;
                }

                return WoWPoint.Empty;
            }
        }

        public override int NormalIcon
        {
            get { return areaPoi_0.NormalIcon; }
        }

        public override int MapId
        {
            get { return areaPoi_0.MapID; }
        }

        public override int AreaId
        {
            get { return areaPoi_0.AreaID; }
        }

        public override uint WorldState
        {
            get { return GetWorldState(areaPoi_0.WorldStateID); }
        }

        public override bool ShowInBattleMap
        {
            get { return areaPoi_0.ShowInBattleMap; }
        }

        public override bool ShowInBg
        {
            get { return areaPoi_0.ShowInBg; }
        }

        public override bool ShowInZone
        {
            get { return areaPoi_0.ShowInZone; }
        }

        private static uint GetWorldState(uint uint_0)
        {
            Executor executor = StyxWoW.Memory.Executor;
            if (executor == null)
            {
                throw new Exception("Invalid executor in GetWorldState");
            }

            try
            {
                lock (executor.AssemblyLock)
                {
                    executor.Clear();
                    executor.AddRandomLine("push {0}", new object[] { uint_0 });
                    executor.AddRandomLine("call {0}", new object[] { StyxWoW.Offsets.Functions.GetWorldState });
                    executor.AddRandomLine("add esp, 4", new object[0]);
                    executor.AddRandomLine("retn", new object[0]);
                    executor.Execute();

                    using (StyxWoW.Memory.TemporaryCacheState(false))
                    {
                        return executor.Memory.Read<uint>(executor.ReturnPointer);
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.WriteException(ex);
            }

            return 0U;
        }

        private readonly AreaPoi areaPoi_0;

        private WoWPoint woWPoint_0;
    }
}
