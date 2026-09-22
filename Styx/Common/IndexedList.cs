using System.Collections.Generic;

namespace Styx.Common
{
    public class IndexedList<T> : List<T>
    {
        private int _index;

        public IndexedList(bool isCyclic = false)
        {
            IsCyclic = isCyclic;
        }

        public IndexedList(int capacity, bool isCyclic = false)
            : base(capacity)
        {
            IsCyclic = isCyclic;
        }

        public IndexedList(IEnumerable<T> collection, bool isCyclic = false)
            : base(collection)
        {
            IsCyclic = isCyclic;
        }

        public bool IsCyclic { get; set; }

        public int Index
        {
            get
            {
                NormalizeIndex();
                return _index;
            }
            set
            {
                _index = value;
                NormalizeIndex();
            }
        }

        public T CurrentOrDefault
        {
            get { return Count > 0 ? this[Index] : default(T); }
        }

        public T Current
        {
            get { return this[Index]; }
        }

        public bool Next()
        {
            int index = Index;
            Index++;
            return IsCyclic || Index == index + 1;
        }

        public bool Previous()
        {
            int index = Index;
            Index--;
            return IsCyclic || Index == index - 1;
        }

        private void NormalizeIndex()
        {
            if (_index == 0 || (_index >= 0 && _index < Count))
                return;
            if (Count == 0)
            {
                _index = 0;
                return;
            }
            if (!IsCyclic)
            {
                _index = Clamp(0, Count - 1, _index);
                return;
            }
            if (_index < 0)
            {
                int offset = -_index - 1;
                offset %= Count;
                _index = Count - 1 - offset;
                return;
            }
            _index %= Count;
        }

        private static int Clamp(int min, int max, int value)
        {
            if (value < min)
                return min;
            return value <= max ? value : max;
        }
    }
}
