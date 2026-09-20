using System;
using System.Runtime.CompilerServices;

namespace Styx.Common
{
    public struct Range : IEquatable<Range>
    {
        public int Lower { get; set; }
        public int Higher { get; set; }

        public Range(int lower, int higher)
            : this()
        {
            Lower = lower;
            Higher = higher;
        }

        public override int GetHashCode()
        {
            return unchecked(Lower.GetHashCode() ^ Higher.GetHashCode());
        }

        #region IEquatable<Range> Members

        public bool Equals(Range other)
        {
            return Lower == other.Lower && Higher == other.Higher;
        }

        #endregion
    }
}
