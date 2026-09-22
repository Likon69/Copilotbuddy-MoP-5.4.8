using System;

namespace Styx.Pathing.FlightorNavigation
{
	internal class FastBinaryHeap<T> where T : IComparable<T>
	{
		public T[] Nodes;
		public int Size;

		public FastBinaryHeap(int capacity)
		{
			Nodes = new T[capacity];
		}

		public void Clear()
		{
			Size = 0;
		}

		public T Top()
		{
			return Nodes[0];
		}

		public T Pop()
		{
			T result = Nodes[0];
			Size--;
			TrickleDown(0, Nodes[Size]);
			return result;
		}

		public void Push(T node)
		{
			Size++;
			BubbleUp(Size - 1, node);
		}

		public void Modify(T item)
		{
			for (int i = 0; i < Size; i++)
			{
				if (ReferenceEquals(item, Nodes[i]))
				{
					BubbleUp(i, item);
					return;
				}
			}
		}

		private void BubbleUp(int i, T node)
		{
			int par = (i - 1) / 2;
			while (i > 0 && Nodes[par].CompareTo(node) > 0)
			{
				Nodes[i] = Nodes[par];
				i = par;
				par = (i - 1) / 2;
			}
			Nodes[i] = node;
		}

		private void TrickleDown(int i, T node)
		{
			int child = i * 2 + 1;
			while (child < Size)
			{
				if (child + 1 < Size && Nodes[child].CompareTo(Nodes[child + 1]) > 0)
				{
					child++;
				}
				Nodes[i] = Nodes[child];
				i = child;
				child = i * 2 + 1;
			}

			BubbleUp(i, node);
		}
	}
}
