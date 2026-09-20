using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Styx.WoWInternals
{
    public class WoWCurrency
    {
        private WoWCurrency(IntPtr ptr)
        {
            if (ptr != (IntPtr)0)
            {
                _ptr = ptr;
                _entry = StyxWoW.Memory.Read<WoWCurrencyEntry>(ptr);
                if (_entry.Info != (IntPtr)0)
                {
                    _info = StyxWoW.Memory.Read<WoWCurrencyInfo>(_entry.Info);
                }
            }
        }

        #region Static Methods

        public static WoWCurrency GetCurrencyById(uint id)
        {
            IntPtr current = StyxWoW.Memory.Read<IntPtr>(StyxWoW.Offsets.Statics.CurrencyInfo);
            while (current != IntPtr.Zero && ((int)current & 1) == 0)
            {
                WoWCurrency currency = new WoWCurrency(current);
                if (currency.Entry == id)
                {
                    return currency;
                }

                current = currency._entry.Next;
            }

            return null;
        }

        public static WoWCurrency GetCurrencyByType(WoWCurrencyType type)
        {
            return GetCurrencyById((uint)type);
        }

        #endregion

        #region Instance Property's

        public bool IsValid
        {
            get { return _ptr != (IntPtr)0; }
        }

        public uint Entry
        {
            get { return _info.Entry; }
        }

        public string Name
        {
            get { return StyxWoW.Memory.ReadString(_info.Name, Encoding.UTF8); }
        }

        public uint CurrencyCategoryEntry
        {
            get { return _info.CurrencyCategoryEntry; }
        }

        public uint Amount
        {
            get { return _entry.Amount; }
        }

        public uint TotalMax
        {
            get { return _info.TotalMax; }
        }

        public uint WeeklyMax
        {
            get
            {
                if ((_info.Flags & 4) == 0)
                {
                    return _info.WeeklyMax;
                }

                return _entry.WeeklyMax;
            }
        }

        public WoWCurrencyType CurrencyType
        {
            get { return (WoWCurrencyType)Entry; }
        }

        #endregion

        #region Overrides of Object

        public override string ToString()
        {
            return string.Format("[{0}] Entry:{1} CategoryEntry:{2} Type:{3} Amount:{4} TotalMax:{5} WeeklyMax:{6}", new object[] { Name, Entry, CurrencyCategoryEntry, CurrencyType, Amount, TotalMax, WeeklyMax });
        }

        #endregion

        private readonly IntPtr _ptr;

        private WoWCurrencyEntry _entry;

        private WoWCurrencyInfo _info;

        #region Nested Type: WoWCurrencyEntry

        private struct WoWCurrencyEntry
        {
            private readonly uint _unk;

            public IntPtr Next;

            public IntPtr Info;

            public uint Amount;

            private readonly uint signed10;

            public uint WeeklyMax;
        }

        #endregion

        #region Nested Type: WoWWCurrencyInfo

        public struct WoWCurrencyInfo
        {
            public uint Entry;

            public uint CurrencyCategoryEntry;

            public IntPtr Name;

            private readonly uint _unk3;

            private readonly uint _unk4;

            private readonly uint _unk5;

            private readonly uint _unk6;

            public uint TotalMax;

            public uint WeeklyMax;

            public byte Flags;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            private readonly char[] _padding;
        }

        #endregion
    }
}
