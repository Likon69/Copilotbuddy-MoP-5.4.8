using System;
using System.Collections;
using System.Collections.Generic;

namespace Styx.Common
{
    public class DualHashSet<T1, T2> : IEnumerable<object>
    {
        public HashSet<T1> HashSet1 { get; private set; }
        public HashSet<T2> HashSet2 { get; private set; }

        public int Count
        {
            get
            {
                return HashSet1.Count + HashSet2.Count;
            }
        }

        public DualHashSet()
        {
            HashSet1 = new HashSet<T1>();
            HashSet2 = new HashSet<T2>();
        }

        public bool Add(T1 item)
        {
            return HashSet1.Add(item);
        }

        public bool Add(T2 item)
        {
            return HashSet2.Add(item);
        }

        public void Add(ValuePair<T1, T2> item)
        {
            HashSet1.Add(item.Value1);
            HashSet2.Add(item.Value2);
        }

        public bool Remove(T1 item)
        {
            return HashSet1.Remove(item);
        }

        public bool Remove(T2 item)
        {
            return HashSet2.Remove(item);
        }

        public bool Contains(T1 item)
        {
            return HashSet1.Contains(item);
        }

        public bool Contains(T2 item)
        {
            return HashSet2.Contains(item);
        }

        public bool Contains(ValuePair<T1, T2> vp)
        {
            return Contains(vp.Value1) || Contains(vp.Value2);
        }

        public IEnumerator<object> GetEnumerator()
        {
            foreach (object obj in HashSet1)
                yield return obj;

            foreach (object obj in HashSet2)
                yield return obj;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
