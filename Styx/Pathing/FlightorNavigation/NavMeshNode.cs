using Styx.Common;

namespace Styx.Pathing.FlightorNavigation
{
	internal struct NavMeshNode
	{
		public NavMeshNode(Vector2 location, int[] neighbors)
		{
			Location = location;
			Neighbors = neighbors;
		}

		public Vector2 Location;
		public int[] Neighbors;
	}
}
