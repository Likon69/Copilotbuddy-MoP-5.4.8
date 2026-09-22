using System;

namespace Styx.Pathing.FlightorNavigation
{
	[Flags]
	internal enum PathFindNodeFlags
	{
		Open = 1,
	}

	internal class PathFindNode : IComparable<PathFindNode>
	{
		public PathFindNode(int nodeRef, int previousNodeRef, float cost, float totalCost)
		{
			NodeRef = nodeRef;
			PreviousNodeRef = previousNodeRef;
			Cost = cost;
			TotalCost = totalCost;
		}

		public int NodeRef;
		public int PreviousNodeRef;
		public float Cost;
		public float TotalCost;
		public PathFindNodeFlags Flags;

		public int CompareTo(PathFindNode other)
		{
			return TotalCost.CompareTo(other.TotalCost);
		}
	}
}
