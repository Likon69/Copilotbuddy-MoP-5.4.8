using System;
using System.Runtime.InteropServices;
using System.Text;
using GreenMagic;
using Styx.WoWInternals.WoWObjects;

namespace Styx.WoWInternals
{
    public class WoWTotemInfo
    {
        internal WoWTotemInfo(int slot)
        {
            IntPtr totemInfoArrayPtr = StyxWoW.Offsets.Statics.TotemInfos;
            _info = StyxWoW.Memory.Read<NativeTotemInfo>(totemInfoArrayPtr + slot * MarshalCache<NativeTotemInfo>.Size);
        }

        public ulong Guid
        {
            get { return _info.Guid; }
        }

        public bool Expired
        {
            get { return Expires <= DateTime.Now; }
        }

        public WoWUnit Unit
        {
            get { return ObjectManager.GetObjectByGuid<WoWUnit>(Guid); }
        }

        public uint Slot
        {
            get { return _info.Slot; }
        }

        public uint Duration
        {
            get { return _info.Duration; }
        }

        public DateTime StartTime
        {
            get { return StyxWoW.WoWClient.PerformanceCounterToDateTime((ulong)_info.StartTime); }
        }

        public DateTime Expires
        {
            get { return StartTime.AddMilliseconds(Duration); }
        }

        public WoWTotem WoWTotem
        {
            get
            {
                if (Unit == null)
                {
                    return WoWTotem.None;
                }

                return (ulong)((ulong)((long)Type) << 32) + (WoWTotem)Unit.CreatedBySpellId;
            }
        }

        public WoWSpell Spell
        {
            get { return WoWSpell.FromId(WoWTotem.GetTotemSpellId()); }
        }

        public string IconPath
        {
            get { return StyxWoW.Memory.ReadString(_info.IconPtr, Encoding.UTF8); }
        }

        public string Name
        {
            get { return StyxWoW.Memory.ReadString(_info.NamePtr, Encoding.UTF8); }
        }

        public WoWTotemType Type
        {
            get { return (int)Slot + WoWTotemType.Fire; }
        }

        private NativeTotemInfo _info;

        #region Nested type: NativeTotemInfo

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct NativeTotemInfo
        {
            internal readonly uint Slot;

            internal readonly uint Dword4;

            internal readonly ulong Guid;

            internal readonly IntPtr NamePtr;

            internal readonly uint Duration;

            internal readonly uint StartTime;

            internal readonly IntPtr IconPtr;
        }

        #endregion
    }
}
