using System;

namespace Styx.CommonBot.Inventory
{
    public class WoWPrice
    {
        public WoWPrice(long copper)
        {
            _totalCoppers = copper;
            Gold = copper / 10000L;
            long num = copper - Gold * 10000L;
            Silver = num / 100L;
            num -= Silver * 100L;
            Copper = num;
        }

        public long TotalCoppers
        {
            get { return _totalCoppers; }
        }

        public long Gold { get; private set; }

        public long Silver { get; private set; }

        public long Copper { get; private set; }

        public static WoWPrice operator +(WoWPrice left, WoWPrice right)
        {
            return new WoWPrice(left._totalCoppers + right.TotalCoppers);
        }

        public static WoWPrice operator -(WoWPrice left, WoWPrice right)
        {
            return new WoWPrice(left._totalCoppers - right.TotalCoppers);
        }

        public static WoWPrice operator *(WoWPrice left, WoWPrice right)
        {
            return new WoWPrice(left._totalCoppers * right.TotalCoppers);
        }

        public static WoWPrice operator /(WoWPrice left, WoWPrice right)
        {
            return new WoWPrice(left._totalCoppers / right.TotalCoppers);
        }

        public static WoWPrice operator +(WoWPrice left, long copper)
        {
            return new WoWPrice(left._totalCoppers + copper);
        }

        public static WoWPrice operator -(WoWPrice left, long copper)
        {
            return new WoWPrice(left._totalCoppers - copper);
        }

        public static WoWPrice operator *(WoWPrice left, long copper)
        {
            return new WoWPrice(left._totalCoppers * copper);
        }

        public static WoWPrice operator *(WoWPrice left, double factor)
        {
            return new WoWPrice((long)((double)left._totalCoppers * factor));
        }

        public static WoWPrice operator /(WoWPrice left, long copper)
        {
            return new WoWPrice(left._totalCoppers / copper);
        }

        public static WoWPrice operator /(WoWPrice left, double factor)
        {
            return new WoWPrice(left._totalCoppers / (long)factor);
        }

        public static bool operator <(WoWPrice left, WoWPrice right)
        {
            return left.TotalCoppers < right.TotalCoppers;
        }

        public static bool operator >(WoWPrice left, WoWPrice right)
        {
            return left.TotalCoppers > right.TotalCoppers;
        }

        public static bool operator <=(WoWPrice left, WoWPrice right)
        {
            return left.TotalCoppers <= right.TotalCoppers;
        }

        public static bool operator >=(WoWPrice left, WoWPrice right)
        {
            return left.TotalCoppers >= right.TotalCoppers;
        }

        public override string ToString()
        {
            return string.Format("{0}g{1}s{2}c", Gold, Silver, Copper);
        }

        private readonly long _totalCoppers;
    }
}
