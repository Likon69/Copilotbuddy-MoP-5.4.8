using System;

namespace Styx.CommonBot.Profiles
{
    public static class VendorTypeExtensions
    {
        public static UnitNPCFlags AsNpcFlag(this Vendor.VendorType vt)
        {
            switch (vt)
            {
                case Vendor.VendorType.Repair:
                    return UnitNPCFlags.Repair;
                case Vendor.VendorType.Food:
                    return UnitNPCFlags.FoodVendor;
                case Vendor.VendorType.Sell:
                    return UnitNPCFlags.AnyVendor;
                case Vendor.VendorType.Train:
                    return UnitNPCFlags.ClassTrainer;
                case Vendor.VendorType.FlightMaster:
                    return UnitNPCFlags.Flightmaster;
            }
            return UnitNPCFlags.None;
        }
    }
}
