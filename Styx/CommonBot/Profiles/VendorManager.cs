using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using Styx.Common;
using Styx.CommonBot.Database;
using Styx.CommonBot.ObjectDatabase;

namespace Styx.CommonBot.Profiles
{
    public class VendorManager
    {
        private readonly List<Vendor> _vendors;

        public VendorManager()
        {
            Blacklist = new HashSet<Vendor>();
            ForcedVendors = new List<Vendor>();
            _vendors = new List<Vendor>();
        }

        public VendorManager(XElement element) : this()
        {
            _vendors = new List<Vendor>();
            List<XElement> vendors = element.Elements().ToList();
            foreach (XElement vendor in vendors)
            {
                try
                {
                    if (vendor.Name == "Vendor")
                    {
                        var curVendor = new Vendor(vendor);
                        _vendors.Add(curVendor);
                    }
                    else
                    {
                        throw new ProfileUnknownElementException(vendor, "Vendor");
                    }
                }
                catch (ProfileException ex)
                {
                    Logging.WriteException(ex);
                }
            }
        }

        /// <summary>
        /// List of vendors you can set to 'override' the current vendors defined by the profile.
        /// </summary>
        public List<Vendor> ForcedVendors { get; set; }

        /// <summary>
        /// All the vendors.
        /// </summary>
        public List<Vendor> AllVendors
        {
            get { return _vendors; }
        }

        public Lookup<Vendor.VendorType, Vendor> Vendors
        {
            get
            {
                if (_vendors == null)
                    return null;

                EnsureBlacklist();
                return (Lookup<Vendor.VendorType, Vendor>)_vendors.ToLookup(vend => vend.Type);
            }
        }

        public HashSet<Vendor> Blacklist { get; private set; }

        public Vendor GetClosestVendor()
        {
            return GetClosestVendor(Vendor.VendorType.Unknown);
        }

        public Vendor GetClosestVendor(Vendor.VendorType type)
        {
            try
            {
                EnsureBlacklist();

                List<Vendor> vendors = ForcedVendors != null && ForcedVendors.Count > 0
                                            ? ForcedVendors.Where(vendor => vendor.UsableWhen == null || vendor.UsableWhen()).ToList()
                                            : _vendors.Where(vendor => vendor.UsableWhen == null || vendor.UsableWhen()).ToList();

                var filteredVendors = new List<Vendor>();
                if (type != Vendor.VendorType.Unknown)
                {
                    if (type != Vendor.VendorType.Sell)
                    {
                        filteredVendors.AddRange(vendors.Where(v => v.Type == type));
                    }
                    else
                    {
                        filteredVendors.AddRange(vendors.Where(v => v.Type == Vendor.VendorType.Repair || v.Type == Vendor.VendorType.Sell));
                    }
                }
                else
                {
                    filteredVendors.AddRange(vendors);
                }

                Vendor closest;
                if (filteredVendors.Count == 0)
                {
                    try
                    {
                        NpcResult nearestNpc = Query.GetNearestNpc(StyxWoW.Me.MapId, StyxWoW.Me.Location, type.AsNpcFlag());
                        if (nearestNpc != null)
                        {
                            NavType? navType = Styx.CommonBot.Vendors.NavType;
                            return new Vendor(nearestNpc.Entry, nearestNpc.Name, type, nearestNpc.Location, navType != null ? navType.Value : NavType.Run);
                        }
                    }
                    catch (Exception e)
                    {
                        Logging.Write(e.ToString());
                    }

                    return null;
                }

                WoWPoint myLoc = StyxWoW.Me.Location;
                WoWClass myClass = StyxWoW.Me.Class;

                filteredVendors.RemoveAll(v => v.Type == Vendor.VendorType.Train && v.TrainClass != myClass);
                closest = filteredVendors.OrderBy(v => v.Location.DistanceSqr(myLoc)).FirstOrDefault();

                return closest;
            }
            catch (Exception e)
            {
                Logging.WriteException(e);
                return null;
            }
        }

        private void EnsureBlacklist()
        {
            if (_vendors == null)
            {
                return;
            }

            _vendors.RemoveAll(vend => Blacklist.Contains(vend));

            if (ForcedVendors != null)
            {
                ForcedVendors.RemoveAll(vend => Blacklist.Contains(vend));
            }
        }
    }
}
