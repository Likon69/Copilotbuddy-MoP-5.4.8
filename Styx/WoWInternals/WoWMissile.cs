using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Styx.WoWInternals.WoWObjects;

namespace Styx.WoWInternals
{
    public class WoWMissile
    {
        internal struct CMissile
        {
            public ulong CasterGuid;

            public ulong Unknown01;

            public ulong TargetGuid;

            public ulong Unknown02;

            public uint Unknown03;

            public int SpellId;

            public uint Unknown04;

            public int SpellVisualId;

            public uint Unknown05;

            public uint Unknown06;

            public uint Unknown07;

            public float Unknown08;

            public WoWPoint Pos;

            public WoWPoint FirePos;

            public WoWPoint ImpactPos;

            public WoWPoint Unknown09;

            public uint Unknown10;

            public uint Unknown11;

            public uint Unknown12;

            public WoWPoint Unknown13;

            public uint Flags;

            public uint Unknown14;

            public WoWPoint Unknown15;

            public uint Unknown16;

            public uint Unknown17;

            public uint Unknown18;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 11, ArraySubType = UnmanagedType.U4)]
            public uint[] Unknown19;

            public uint Unknown20;

            public IntPtr Unknown21;

            public uint Unknown22;

            public uint Unknown23;

            public uint Unknown24;

            public IntPtr Unknown25;

            public uint Unknown26;

            public uint Unknown27;

            public uint Unknown28;

            public uint Unknown29;

            public uint Unknown30;

            public uint Unknown31;

            public uint Unknown32;

            public uint Unknown33;

            public uint Unknown34;

            public uint Unknown35;

            public float Unknown36;

            public uint Unknown37;

            public uint Unknown38;

            public uint Unknown39;

            public uint Unknown40;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.U4)]
            public uint[] Unknown41;

            public uint Unknown42;

            public uint Unknown43;

            public uint Unknown44;

            public uint Unknown45;

            public uint Unknown46;

            public uint Unknown47;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.U4)]
            public uint[] Unknown48;

            public WoWPoint Unknown49;

            public uint Unknown50;

            public uint Unknown51;

            public IntPtr Unknown52;

            public IntPtr Unknown53;

            public uint Unknown54;

            public ulong Unknown55;

            public uint Unknown56;

            public uint Unknown57;

            public uint Unknown58;

            public float Unknown59;

            public WoWPoint Unknown60;

            public uint Unknown61;

            public uint Unknown62;

            public uint Unknown63;

            public uint Unknown64;

            public uint Unknown65;

            public uint Unknown66;

            public uint Unknown67;

            public uint Unknown68;

            public uint Unknown69;

            public uint Unknown70;

            public uint Unknown71;

            public uint Unknown72;

            public uint Unknown73;

            public uint Unknown74;
        }

        public IntPtr BaseAddress { get; private set; }

        internal CMissile Native => StyxWoW.Memory.Read<CMissile>(BaseAddress);

        internal IntPtr NextMissile => StyxWoW.Memory.Read<IntPtr>(BaseAddress + 376);

        public ulong CasterGuid => Native.CasterGuid;

        public ulong TargetGuid => Native.TargetGuid;

        public int SpellId => Native.SpellId;

        public int SpellVisualId => Native.SpellVisualId;

        public uint Flags => Native.Flags;

        public WoWPoint Position => Native.Pos;

        public WoWPoint FirePosition => Native.FirePos;

        public WoWPoint ImpactPosition => Native.ImpactPos;

        public WoWUnit Caster => ObjectManager.GetObjectByGuid<WoWUnit>(CasterGuid);

        public WoWUnit Target => ObjectManager.GetObjectByGuid<WoWUnit>(TargetGuid);

        public WoWSpell Spell => WoWSpell.FromId(SpellId);

        public static IEnumerable<WoWMissile> InFlightMissiles
        {
            get
            {
                IntPtr missilePtr = StyxWoW.Memory.Read<IntPtr>(StyxWoW.Offsets.Statics.InFlightMissiles);
                while (missilePtr != IntPtr.Zero)
                {
                    WoWMissile missile = new WoWMissile(missilePtr);
                    yield return missile;
                    missilePtr = missile.NextMissile;
                }
            }
        }

        public static IEnumerable<WoWMissile> ImpactedMissiles
        {
            get
            {
                IntPtr missilePtr = StyxWoW.Memory.Read<IntPtr>(StyxWoW.Offsets.Statics.InFlightMissiles + 4);
                while (missilePtr != IntPtr.Zero)
                {
                    yield return new WoWMissile(missilePtr);
                    missilePtr = StyxWoW.Memory.Read<IntPtr>(missilePtr + 376);
                }
            }
        }

        public static IEnumerable<WoWMissile> AllMissiles => InFlightMissiles.Union(ImpactedMissiles);

        private WoWMissile(IntPtr ptr)
        {
            BaseAddress = ptr;
        }
    }
}
