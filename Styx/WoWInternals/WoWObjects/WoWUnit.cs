using System;

namespace Styx.WoWInternals.WoWObjects
{
    public class WoWUnit : WoWObject
    {
        public WoWUnit(IntPtr baseAddress)
            : base(baseAddress)
        {
        }

        public override WoWPoint Location
        {
            get
            {
                EnsureValid("Location");
                if (IsOnTransport)
                {
                    return base.Location;
                }

                return RelativeLocation;
            }
        }

        public override WoWPoint RelativeLocation
        {
            get
            {
                EnsureValid("RelativeLocation");
                return MovementInfo.Position;
            }
        }

        public ulong TransportGuid
        {
            get
            {
                EnsureValid("TransportGuid");
                return MovementInfo.TransportGuid;
            }
        }

        public bool IsOnTransport
        {
            get { return TransportGuid != 0UL; }
        }

        public WoWMovementInfo MovementInfo
        {
            get
            {
                EnsureValid("MovementInfo");
                IntPtr baseAddress = BaseAddress + (int)StyxWoW.Offsets.Misc.WoWUnitMovementDataDirect;
                if (_movementInfo == null || _movementInfo.BaseAddress != baseAddress)
                {
                    _movementInfo = new WoWMovementInfo(baseAddress);
                }

                return _movementInfo;
            }
        }

        private WoWMovementInfo _movementInfo;
    }
}
