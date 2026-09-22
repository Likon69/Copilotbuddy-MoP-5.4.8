using System;
using System.Linq;

using Styx.Common;

namespace Bots.BGBuddy.HeatMap
{
    internal class ObjectiveHeatpoint : HeatPoint
    {
        // This class is just to help track if it's an objective, or a player, etc.
        // Could change to a bool, but if we end up doing something different, then we need to use this class anyway!
        public ObjectiveHeatpoint(float x, float y, float radius, float intensity) : base(x, y, radius, intensity)
        {
        }
    }

    internal class HeatPoint : IEquatable<HeatPoint>
    {
        public readonly float Radius;
        public readonly float X;
        public readonly float Y;
        private float _intensity;

        /// <summary>
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="radius"></param>
        /// <param name="intensity">A value between 0.0 and 1.0 determining the intensity of the heat spot</param>
        public HeatPoint(float x, float y, float radius, float intensity)
        {
            X = x;
            Y = y;
            Radius = radius;
            Intensity = intensity;
        }

        public float Intensity { get { return _intensity; } private set { _intensity = MathEx.Clamp(-1, 1, value); } }

        public bool Equals(HeatPoint other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return Radius.Equals(other.Radius) && X.Equals(other.X) && Y.Equals(other.Y) &&
                   _intensity.Equals(other._intensity);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals((HeatPoint) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Radius.GetHashCode();
                hashCode = (hashCode * 397) ^ X.GetHashCode();
                hashCode = (hashCode * 397) ^ Y.GetHashCode();
                hashCode = (hashCode * 397) ^ _intensity.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(HeatPoint left, HeatPoint right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(HeatPoint left, HeatPoint right)
        {
            return !Equals(left, right);
        }

        public float GetIntensity(float distance)
        {
            // Quick check to filter out others. Perf increase over extra instructions when not needed.
            if (distance > Radius)
                return 0;

            return MathEx.Lerp(0, Intensity, MathEx.InverseLerp(Radius, 0, distance));
        }

        public float GetIntensity(Vector2 from)
        {
            return GetIntensity(from.Distance(this));
        }

        public static implicit operator Vector2(HeatPoint hp)
        {
            return new Vector2(hp.X, hp.Y);
        }

        public float GetHeatAverage()
        {
            return Intensity + Heatmap.HeatPoints.Where(hp => hp != this).Sum(hp => GetIntensity(hp));
        }
    }
}
