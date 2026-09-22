using System;
using System.Collections.Generic;
using System.Linq;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.Profiles;

namespace Styx.Pathing.FlightorNavigation
{
	public static class BlackspotManager
	{
		static BlackspotManager()
		{
			Styx.CommonBot.BotEvents.Profile.OnNewProfileLoaded += BlackspotManager.OnNewProfileLoaded;
			Styx.CommonBot.Profiles.Profile currentProfile = Styx.CommonBot.Profiles.ProfileManager.CurrentProfile;
			if (!(currentProfile == null) && currentProfile.AerialBlackspots != null && currentProfile.AerialBlackspots.Count != 0)
			{
				foreach (KeyValuePair<Tuple<uint, WoWFactionGroup>, List<Vector2[]>> keyValuePair in currentProfile.AerialBlackspots)
				{
					BlackspotManager.AddBlackspots(keyValuePair.Key.Item1, keyValuePair.Key.Item2, keyValuePair.Value);
				}
			}
		}

		private static void OnNewProfileLoaded(Styx.CommonBot.BotEvents.Profile.NewProfileLoadedEventArgs newProfileLoadedEventArgs)
		{
			if (newProfileLoadedEventArgs.OldProfile != null && newProfileLoadedEventArgs.OldProfile.AerialBlackspots != null && newProfileLoadedEventArgs.OldProfile.AerialBlackspots.Count != 0)
			{
				foreach (KeyValuePair<Tuple<uint, WoWFactionGroup>, List<Vector2[]>> keyValuePair in newProfileLoadedEventArgs.OldProfile.AerialBlackspots)
				{
					BlackspotManager.RemoveBlackspots(keyValuePair.Key.Item1, keyValuePair.Key.Item2, keyValuePair.Value);
				}
			}
			if (newProfileLoadedEventArgs.NewProfile.AerialBlackspots != null && newProfileLoadedEventArgs.NewProfile.AerialBlackspots.Count > 0)
			{
				foreach (KeyValuePair<Tuple<uint, WoWFactionGroup>, List<Vector2[]>> keyValuePair2 in newProfileLoadedEventArgs.NewProfile.AerialBlackspots)
				{
					BlackspotManager.AddBlackspots(keyValuePair2.Key.Item1, keyValuePair2.Key.Item2, keyValuePair2.Value);
				}
			}
		}

		public static IEnumerable<Vector2[]> Blackspots
		{
			get
			{
				uint mapId = StyxWoW.Me.MapId;
				WoWFactionGroup factionGroup = StyxWoW.Me.FactionGroup;
				List<Vector2[]> list = new List<Vector2[]>();
				Tuple<uint, WoWFactionGroup> tuple = new Tuple<uint, WoWFactionGroup>(mapId, WoWFactionGroup.Neutral);
				Tuple<uint, WoWFactionGroup> tuple2 = new Tuple<uint, WoWFactionGroup>(mapId, factionGroup);
				if (BlackspotManager._blackspots.ContainsKey(tuple))
				{
					list.AddRange(BlackspotManager._blackspots[tuple]);
				}
				if (BlackspotManager._blackspots.ContainsKey(tuple2))
				{
					list.AddRange(BlackspotManager._blackspots[tuple2]);
				}
				return list;
			}
		}

		public static bool IsInBlackspot(WoWPoint point)
		{
			return BlackspotManager.Blackspots.Any(polygon => BlackspotManager.PointInPolygon(polygon, new Vector2(point.X, point.Y)));
		}

		private static bool PointInPolygon(Vector2[] polygon, Vector2 point)
		{
			bool flag = false;
			int i = 0;
			int num = polygon.Length - 1;
			while (i < polygon.Length)
			{
				if (polygon[i].Y > point.Y != polygon[num].Y > point.Y && point.X < (polygon[num].X - polygon[i].X) * (point.Y - polygon[i].Y) / (polygon[num].Y - polygon[i].Y) + polygon[i].X)
				{
					flag = !flag;
				}
				num = i++;
			}
			return flag;
		}

		internal static void AddDefaultBlackspots()
		{
			if (BlackspotManager._defaultsAdded)
			{
				return;
			}
			BlackspotManager.AddBlackspots(0U, WoWFactionGroup.Neutral, new List<Vector2[]> { new Vector2[]
			{
				new Vector2(-7546.737f, -1368.555f),
				new Vector2(-7344.393f, -1330.537f),
				new Vector2(-7317.034f, -1210.047f),
				new Vector2(-7336.192f, -955.021f),
				new Vector2(-7335.584f, -797.5026f),
				new Vector2(-7734.629f, -844.2428f),
				new Vector2(-7796.103f, -1083.706f),
				new Vector2(-7787.898f, -1264.005f)
			} });
			BlackspotManager.AddBlackspots(0U, WoWFactionGroup.Horde, new List<Vector2[]> { new Vector2[]
			{
				new Vector2(-9089.791f, 403.6077f),
				new Vector2(-9019.003f, 310.1484f),
				new Vector2(-8884.419f, 370.0167f),
				new Vector2(-8817.055f, 288.0225f),
				new Vector2(-8567.823f, 237.9563f),
				new Vector2(-8372.982f, 74.27246f),
				new Vector2(-8067.143f, 259.4106f),
				new Vector2(-7942.156f, 896.7501f),
				new Vector2(-8023.603f, 1172.448f),
				new Vector2(-7952.16f, 1459.715f),
				new Vector2(-8071.096f, 1412.827f),
				new Vector2(-8175.608f, 1279.717f),
				new Vector2(-8626.808f, 1248.927f),
				new Vector2(-9274.56f, 958.4016f)
			} });
			BlackspotManager.AddBlackspots(1U, WoWFactionGroup.Alliance, new List<Vector2[]> { new Vector2[]
			{
				new Vector2(1348.726f, -4423.85f),
				new Vector2(1406.258f, -4513.621f),
				new Vector2(1611.74f, -4531.624f),
				new Vector2(1730.437f, -4574.095f),
				new Vector2(1909.313f, -4823.78f),
				new Vector2(2036.518f, -4903.274f),
				new Vector2(2197.151f, -4912.26f),
				new Vector2(2506.568f, -4971.552f),
				new Vector2(2640.775f, -4802.936f),
				new Vector2(2431.873f, -4646.192f),
				new Vector2(2161.392f, -4476.597f),
				new Vector2(2129.687f, -4190.782f),
				new Vector2(2075.95f, -4076.873f),
				new Vector2(1893.812f, -4042.157f),
				new Vector2(1792.939f, -3811.462f),
				new Vector2(1385.134f, -4023.502f),
				new Vector2(1338.027f, -4225.851f)
			} });
			BlackspotManager.AddBlackspots(530U, WoWFactionGroup.Alliance, new List<Vector2[]> { new Vector2[]
			{
				new Vector2(-517.6315f, 4136.477f),
				new Vector2(-529.9949f, 4182.617f),
				new Vector2(-563.7723f, 4216.395f),
				new Vector2(-609.9131f, 4228.758f),
				new Vector2(-656.0539f, 4216.395f),
				new Vector2(-689.8313f, 4182.617f),
				new Vector2(-702.1947f, 4136.477f),
				new Vector2(-689.8313f, 4090.336f),
				new Vector2(-656.0539f, 4056.558f),
				new Vector2(-609.9131f, 4044.195f),
				new Vector2(-563.7723f, 4056.558f),
				new Vector2(-529.9948f, 4090.336f)
			} });
			BlackspotManager.AddBlackspots(571U, WoWFactionGroup.Neutral, new List<Vector2[]> { new Vector2[]
			{
				new Vector2(2786.607f, 6277.201f),
				new Vector2(2772.39f, 6290.771f),
				new Vector2(2739.774f, 6255.286f),
				new Vector2(2735.843f, 6129.687f),
				new Vector2(2766.215f, 6093.395f),
				new Vector2(2790.594f, 6104.951f),
				new Vector2(2901.596f, 6077.773f),
				new Vector2(2942.01f, 6117.438f),
				new Vector2(2907.869f, 6155.327f),
				new Vector2(2925.325f, 6202.404f),
				new Vector2(2908.878f, 6251.97f),
				new Vector2(2845.846f, 6265.337f),
				new Vector2(2807.114f, 6254.024f)
			} });
			BlackspotManager.AddBlackspots(870U, WoWFactionGroup.Horde, new List<Vector2[]>
			{
				new Vector2[]
				{
					new Vector2(-178.0362f, -1632.285f),
					new Vector2(-239.2149f, -1577.494f),
					new Vector2(-346.6604f, -1596.989f),
					new Vector2(-427.4664f, -1701.919f),
					new Vector2(-518.568f, -1888.001f),
					new Vector2(-471.2889f, -1963.058f),
					new Vector2(-221.2071f, -1899.88f),
					new Vector2(-160.8047f, -1819.973f)
				},
				new Vector2[]
				{
					new Vector2(-186.2613f, -2405.539f),
					new Vector2(-263.0333f, -2450.284f),
					new Vector2(-329.8753f, -2502.875f),
					new Vector2(-408.8562f, -2583.919f),
					new Vector2(-430.0616f, -2647.971f),
					new Vector2(-502.484f, -2709.325f),
					new Vector2(-491.1442f, -2847.358f),
					new Vector2(-412.208f, -2943.061f),
					new Vector2(-265.2892f, -2812.823f),
					new Vector2(-171.9101f, -2860.382f),
					new Vector2(-42.53977f, -2834.448f),
					new Vector2(-15.08483f, -2749.818f),
					new Vector2(-21.95264f, -2504.257f),
					new Vector2(-148.9079f, -2388.644f)
				}
			});
			BlackspotManager._defaultsAdded = true;
		}

		public static void AddBlackspot(uint mapId, WoWFactionGroup faction, Vector2[] blackspot)
		{
			Tuple<uint, WoWFactionGroup> tuple = new Tuple<uint, WoWFactionGroup>(mapId, faction);
			if (!BlackspotManager._blackspots.ContainsKey(tuple))
			{
				BlackspotManager._blackspots.Add(tuple, new List<Vector2[]>());
			}
			List<Vector2[]> list = BlackspotManager._blackspots[tuple];
			if (!list.Contains(blackspot))
			{
				list.Add(blackspot);
				Flightor.Clear();
			}
		}

		public static void AddBlackspots(uint mapId, WoWFactionGroup faction, IEnumerable<Vector2[]> blackspots)
		{
			foreach (Vector2[] blackspot in blackspots)
			{
				BlackspotManager.AddBlackspot(mapId, faction, blackspot);
			}
		}

		public static void RemoveBlackspot(uint mapId, WoWFactionGroup faction, Vector2[] blackspot)
		{
			Tuple<uint, WoWFactionGroup> tuple = new Tuple<uint, WoWFactionGroup>(mapId, faction);
			if (!BlackspotManager._blackspots.ContainsKey(tuple))
			{
				return;
			}
			List<Vector2[]> list = BlackspotManager._blackspots[tuple];
			if (!list.Contains(blackspot))
			{
				return;
			}
			list.Remove(blackspot);
		}

		public static void RemoveBlackspots(uint mapId, WoWFactionGroup faction, IEnumerable<Vector2[]> blackspots)
		{
			foreach (Vector2[] blackspot in blackspots)
			{
				BlackspotManager.RemoveBlackspot(mapId, faction, blackspot);
			}
		}

		private static readonly Dictionary<Tuple<uint, WoWFactionGroup>, List<Vector2[]>> _blackspots = new Dictionary<Tuple<uint, WoWFactionGroup>, List<Vector2[]>>();

		private static bool _defaultsAdded;
	}
}
