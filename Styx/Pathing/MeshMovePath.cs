using System;
using Tripper.Navigation;

namespace Styx.Pathing
{
	public class MeshMovePath
	{
		public MeshMovePath(Tripper.Navigation.PathFindResult path)
		{
			this.Path = path;
			this.Index = 0;
		}

		public Tripper.Navigation.PathFindResult Path { get; set; }

		public int Index { get; set; }
	}
}
