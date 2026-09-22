using System;
using System.Collections.Generic;
using Styx;
using Styx.Pathing;
using Styx.WoWInternals;
using Tripper.Tools.Math;

namespace Bots.Quest
{
	internal static class HeightHelper
	{
		internal static bool FindMeshHeight(ref Vector3 pos)
		{
			Vector3 myLoc = StyxWoW.Me.Location;
			float height;
			List<float> heights = Navigator.FindHeights(pos.X, pos.Y);
			if (!FindBestHeightOfPool(myLoc, pos, heights, out height))
			{
				Vector3 dir = myLoc - pos;
				dir.Normalize();

				for (float dist = 1f; dist <= 10f; dist++)
				{
					Vector3 testLoc = pos + dir * dist;
					heights = Navigator.FindHeights(testLoc.X, testLoc.Y);
					if (!FindBestHeightOfPool(myLoc, testLoc, heights, out height))
						continue;

					pos = testLoc;
					pos.Z = height;
					return true;
				}

				for (float xOff = -10f; xOff <= 10f; xOff += 2.5f)
				{
					for (float yOff = -10f; yOff <= 10f; yOff += 2.5f)
					{
						var testLoc = new Vector3(pos.X + xOff, pos.Y + yOff, 0f);
						heights = Navigator.FindHeights(testLoc.X, testLoc.Y);
						if (!FindBestHeightOfPool(myLoc, testLoc, heights, out height))
							continue;

						pos = testLoc;
						pos.Z = height;
						return true;
					}
				}

				return false;
			}

			pos.Z = height;

			return true;
		}

		private static bool FindBestHeightOfPool(Vector3 myLoc, Vector3 endLoc, IList<float> heightPool, out float height)
		{
			height = 0f;
			if (heightPool.Count <= 0)
				return false;

			float bestHeightDist = float.MaxValue;
			for (int i = 0; i < heightPool.Count; i++)
			{
				float dist = Math.Abs(myLoc.Z - heightPool[i]);
				if (dist >= bestHeightDist)
					continue;

				bestHeightDist = dist;
				height = heightPool[i];
			}

			return true;
		}
	}
}