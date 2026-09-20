using System;
using System.Collections.Generic;

namespace Styx.Common
{
    public class RangedDictionary<T> : Dictionary<Range, List<T>> where T : IRangeAble
    {
        public int ValueCount
        {
            get
            {
                int itemCount = 0;
                foreach (List<T> itemList in Values)
                {
                    itemCount += itemList.Count;
                }

                return itemCount;
            }
        }

        public List<T> GetAllValues()
        {
            var items = new List<T>();
            foreach (List<T> itemList in Values)
            {
                items.AddRange(itemList);
            }

            return items;
        }

        public void Add(T item)
        {
            if (ContainsKey(item.GetRange()))
            {
                base[item.GetRange()].Add(item);
            }
            else
            {
                Add(item.GetRange(), new List<T> { item });
            }
        }

        public int RemoveWhereValue(Predicate<T> predicate)
        {
            int removedCount = 0;

            foreach (var kvp in this)
            {
                removedCount += kvp.Value.RemoveAll(predicate);
            }

            return removedCount;
        }

        public bool Remove(T item)
        {
            Range rang = item.GetRange();
            return ContainsKey(rang) && base[rang].Remove(item);
        }

        public void RemoveIndexFromRange(Range range, int index)
        {
            if (ContainsKey(range))
            {
                base[range].RemoveAt(index);
            }
        }

        public bool Contains(T item)
        {
            Range rang = item.GetRange();
            return ContainsKey(rang) && base[rang].Contains(item);
        }

        public List<T> GetByBigRange(Range range)
        {
            var tempItems = new List<T>();

            for (int i = range.Lower; i < range.Higher; i++)
            {
                var tempRange = new Range(i, i + 1);
                if (ContainsKey(tempRange))
                {
                    tempItems.AddRange(base[tempRange]);
                }
            }

            return tempItems;
        }
    }
}
