using System;
using System.Collections.Generic;
using Styx.Common;

namespace Styx.CommonBot.AreaManagement
{
    public abstract class Area : IEquatable<Area>
    {
        private readonly Guid _guid;

        protected Area()
        {
            _guid = Guid.NewGuid();
            CircledHotspots = new CircularQueue<Hotspot>();
            Hotspots = new List<Hotspot>();
        }

        public abstract AreaType Type { get; }
        public Guid Guid { get { return _guid; } }

        #region Hotspots

        public CircularQueue<Hotspot> CircledHotspots { get; set; }
        public List<Hotspot> Hotspots { get; set; }

        public void CycleToNearest()
        {
            if (CircledHotspots != null)
            {
                WoWPoint myLoc = StyxWoW.Me.Location;
                WoWPoint nearest = WoWPoint.Zero;
                float nearestDist = float.MaxValue;
                foreach (WoWPoint pnt in CircledHotspots)
                {
                    float dist = pnt.Distance(myLoc);
                    if (dist < nearestDist)
                    {
                        nearestDist = dist;
                        nearest = pnt;
                    }
                }

                if (nearest != WoWPoint.Zero)
                {
                    CircledHotspots.CycleTo(nearest);
                }
            }
        }

        public Hotspot GetNextHotspot()
        {
            return CircledHotspots.Count > 0 ? CircledHotspots.Dequeue() : null;
        }

        private readonly Random _rand = new Random();

        public Hotspot GetRandomHotspot()
        {
            return Hotspots.Count > 0 ? Hotspots[_rand.Next(0, Hotspots.Count)] : null;
        }

        #endregion

        public static bool operator ==(Area left, Area right)
        {
            if (ReferenceEquals(left, right))
                return true;

            return !ReferenceEquals(left, null) && !ReferenceEquals(right, null) && left._guid == right._guid;
        }

        public static bool operator !=(Area left, Area right)
        {
            return !(left == right);
        }

        #region Implementation of IEquatable<Area>

        public bool Equals(Area other)
        {
            if (ReferenceEquals(this, other))
                return true;

            return !ReferenceEquals(other, null) && _guid == other._guid;
        }

        #endregion

        public override int GetHashCode()
        {
            return _guid.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj) || obj.GetType() != typeof(Area))
                return false;
            if (ReferenceEquals(this, obj))
                return true;

            return Equals((Area)obj);
        }
    }
}
