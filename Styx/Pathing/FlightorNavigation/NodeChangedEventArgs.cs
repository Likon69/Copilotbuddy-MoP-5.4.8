using System;

namespace Styx.Pathing.FlightorNavigation
{
	internal class NodeChangedEventArgs : EventArgs
	{
		public NodeChangedEventArgs(NavMeshBuilderNode node)
		{
			Node = node;
		}

		public NavMeshBuilderNode Node { get; private set; }
	}
}
