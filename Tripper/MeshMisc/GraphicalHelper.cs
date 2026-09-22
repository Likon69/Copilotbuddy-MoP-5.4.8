using Tripper.Tools.Math;

namespace Tripper.MeshMisc
{
	public static class GraphicalHelper
	{
		public static Vector3 ToWow(Vector3 v)
		{
			ToWow(ref v);
			return v;
		}

		public static void ToWow(ref Vector3 v)
		{
			ToWow(ref v, out v);
		}

		public static void ToWow(ref Vector3 detour, out Vector3 wow)
		{
			float z = detour.Y;
			wow.Y = -detour.X;
			wow.X = -detour.Z;
			wow.Z = z;
		}

		public static Vector3 ToDetour(Vector3 v)
		{
			ToDetour(ref v);
			return v;
		}

		public static void ToDetour(ref Vector3 v)
		{
			ToDetour(ref v, out v);
		}

		public static void ToDetour(ref Vector3 wow, out Vector3 detour)
		{
			float z = -wow.X;
			detour.X = -wow.Y;
			detour.Y = wow.Z;
			detour.Z = z;
		}

		public static bool IsVectorContained(ref Vector2 bmin, ref Vector2 bmax, ref Vector2 v)
		{
			return bmin.X <= v.X && v.X <= bmax.X && bmin.Y <= v.Y && v.Y <= bmax.Y;
		}

		public static bool IsVectorContained(Vector2 bmin, Vector2 bmax, Vector2 v)
		{
			return IsVectorContained(ref bmin, ref bmax, ref v);
		}

		public static bool IsVectorContained(ref Vector3 bmin, ref Vector3 bmax, ref Vector3 v)
		{
			return bmin.X <= v.X && v.X <= bmax.X && bmin.Y <= v.Y && v.Y <= bmax.Y && bmin.Z <= v.Z && v.Z <= bmax.Z;
		}

		public static bool IsVectorContained(ref Vector2 bmin, ref Vector2 bmax, ref Vector3 v)
		{
			return bmin.X <= v.X && v.X <= bmax.X && bmin.Y <= v.Y && v.Y <= bmax.Y;
		}

		public static bool IsVectorContained(Vector3 bmin, Vector3 bmax, Vector3 v)
		{
			return IsVectorContained(ref bmin, ref bmax, ref v);
		}

		public static bool IsVectorContained2D(ref Vector3 bmin, ref Vector3 bmax, ref Vector3 v)
		{
			return bmin.X <= v.X && v.X <= bmax.X && bmin.Y <= v.Y && v.Y <= bmax.Y;
		}

		public static bool IsVectorContained2D(Vector3 bmin, Vector3 bmax, Vector3 v)
		{
			return IsVectorContained2D(ref bmin, ref bmax, ref v);
		}

		public static bool TriangleContainsPoint(Vector3[] tri, ref Vector3 point)
		{
			Vector3 v0, v1, v2;
			Vector3.Subtract(ref tri[2], ref tri[0], out v0);
			Vector3.Subtract(ref tri[1], ref tri[0], out v1);
			Vector3.Subtract(ref point, ref tri[0], out v2);

			float dot00 = Vector3.Dot(ref v0, ref v0);
			float dot01 = Vector3.Dot(ref v0, ref v1);
			float dot02 = Vector3.Dot(ref v0, ref v2);
			float dot11 = Vector3.Dot(ref v1, ref v1);
			float dot12 = Vector3.Dot(ref v1, ref v2);

			float invDenom = 1f / (dot00 * dot11 - dot01 * dot01);
			float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
			float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

			return u > 0f && v > 0f && (u + v) < 1f;
		}

		public static void NormalizeBounds(Vector3 corner1, Vector3 corner2, out Vector3 bmin, out Vector3 bmax)
		{
			NormalizeBounds(ref corner1, ref corner2);
			bmin = corner1;
			bmax = corner2;
		}

		public static void NormalizeBounds(ref Vector3 bmin, ref Vector3 bmax)
		{
			if (bmin.X > bmax.X)
				Swap(ref bmin.X, ref bmax.X);

			if (bmin.Y > bmax.Y)
				Swap(ref bmin.Y, ref bmax.Y);

			if (bmin.Z > bmax.Z)
				Swap(ref bmin.Z, ref bmax.Z);
		}

		public static void NormalizeBounds(Vector2 corner1, Vector2 corner2, out Vector2 bmin, out Vector2 bmax)
		{
			NormalizeBounds(ref corner1, ref corner2);
			bmin = corner1;
			bmax = corner2;
		}

		public static void NormalizeBounds(ref Vector2 bmin, ref Vector2 bmax)
		{
			if (bmin.X > bmax.X)
				Swap(ref bmin.X, ref bmax.X);

			if (bmin.Y > bmax.Y)
				Swap(ref bmin.Y, ref bmax.Y);
		}

		public static void Swap<T>(ref T val1, ref T val2)
		{
			T temp = val1;
			val1 = val2;
			val2 = temp;
		}
	}
}
