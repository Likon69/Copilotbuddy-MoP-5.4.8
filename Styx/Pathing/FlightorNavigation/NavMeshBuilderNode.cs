using System.Collections.Generic;
using Styx.Common;

namespace Styx.Pathing.FlightorNavigation
{
	internal struct NavMeshBuilderNode
	{
		public NavMeshBuilderNode(int nodeRef, Vector2 location, List<int> neighbors)
		{
			NodeRef = nodeRef;
			Location = location;
			Neighbors = neighbors;
		}

		public int NodeRef;
		public Vector2 Location;
		public readonly List<int> Neighbors;
	}
}
