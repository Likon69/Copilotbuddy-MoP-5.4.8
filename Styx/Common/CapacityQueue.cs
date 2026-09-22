using System.Collections.Generic;

namespace Styx.Common
{
    public class CapacityQueue<T> : Queue<T>
    {
        public CapacityQueue(int capacity)
            : base(capacity)
        {
            Capacity = capacity;
        }

        public int Capacity { get; private set; }

        public new void Enqueue(T item)
        {
            if (Count + 1 >= Capacity)
                Dequeue();
            base.Enqueue(item);
        }
    }
}
