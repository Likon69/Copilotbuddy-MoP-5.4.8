using System;
using System.Collections.Generic;

namespace Styx.Common
{
    public class CircularQueue<T> : Queue<T>
    {
        public delegate void EndOfQueue(object sender, EventArgs e);
        public event EndOfQueue OnEndOfQueue;

        public delegate void StartOfQueue(object sender, EventArgs e);
        public event StartOfQueue OnStartOfQueue;

        public T First { get; private set; }

        public new T Dequeue()
        {
            T tmp = base.Dequeue();
            Enqueue(tmp);

            if (tmp.Equals(First))
                if (OnStartOfQueue != null)
                    OnStartOfQueue(this, new EventArgs());

            if (First.Equals(Peek()))
                if (OnEndOfQueue != null)
                    OnEndOfQueue(this, new EventArgs());

            return tmp;
        }

        public new void Enqueue(T item)
        {
            if (Equals(First, default(T)))
                First = item;

            base.Enqueue(item);
        }

        public void CycleTo(T item)
        {
            T tmp = Peek();

            for (int i = 0; i < Count; i++)
            {
                if (!tmp.Equals(item))
                {
                    Dequeue();
                    tmp = Peek();
                }
                else
                {
                    return;
                }
            }

            Dequeue();
        }

        public void Add(T item)
        {
            Enqueue(item);
        }
    }
}
