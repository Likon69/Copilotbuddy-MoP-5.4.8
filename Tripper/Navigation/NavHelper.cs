using Tripper.MeshMisc;
using Tripper.Tools.Math;

namespace Tripper.Navigation
{
	/// <summary>
	/// Provides tools required to use a navmesh.
	/// </summary>
	public static class NavHelper
	{
		/// <summary>
		/// Converts a World of Warcraft coordinate into a nav coordinate that can be used with a navmesh.
		/// </summary>
		/// <param name="wow"></param>
		/// <returns></returns>
		public static Vector3 ToNav(Vector3 wow)
		{
			return GraphicalHelper.ToDetour(wow);
		}

		/// <summary>
		/// Converts a World of Warcraft coordinate into a nav coordinate that can be used with a navmesh.
		/// </summary>
		/// <param name="coordinate"></param>
		public static void ToNav(ref Vector3 coordinate)
		{
			GraphicalHelper.ToDetour(ref coordinate);
		}

		/// <summary>
		/// Converts a World of Warcraft coordinate into a nav coordinate that can be used with a navmesh.
		/// </summary>
		/// <param name="wow"></param>
		/// <param name="nav"></param>
		public static void ToNav(ref Vector3 wow, out Vector3 nav)
		{
			GraphicalHelper.ToDetour(ref wow, out nav);
		}

		/// <summary>
		/// Converts a nav coordinate into a World of Warcraft coordinate.
		/// </summary>
		/// <param name="nav"></param>
		/// <returns></returns>
		public static Vector3 ToWow(Vector3 nav)
		{
			return GraphicalHelper.ToWow(nav);
		}

		/// <summary>
		/// Converts a nav coordinate into a World of Warcraft coordinate.
		/// </summary>
		/// <param name="coordinate"></param>
		public static void ToWow(ref Vector3 coordinate)
		{
			GraphicalHelper.ToWow(ref coordinate);
		}

		/// <summary>
		/// Converts a nav coordinate into a World of Warcraft coordinate.
		/// </summary>
		/// <param name="nav"></param>
		/// <param name="wow"></param>
		public static void ToWow(ref Vector3 nav, out Vector3 wow)
		{
			GraphicalHelper.ToWow(ref nav, out wow);
		}
	}
}
