using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using Styx.Common;
using Styx.CommonBot.Profiles;
using Styx.Resources;

namespace Styx.CommonBot.AreaManagement
{
	public class HotspotManager
	{
		private readonly CircularQueue<WoWPoint> _circledHotspots;
		public List<WoWPoint> Hotspots { get; private set; }
		private readonly Dictionary<WoWPoint, DateTime> _blackList = new Dictionary<WoWPoint, DateTime>();

		public Dictionary<WoWPoint, DateTime> Blacklist
		{
			get
			{
				RefreshBlacklist();
				return _blackList;
			}
		}

		public HotspotManager(IEnumerable<WoWPoint> points)
		{
			Hotspots = points.ToList();
		}

		public HotspotManager(XElement element)
		{
			if(Timer == null)
				Timer = new Stopwatch();

			Hotspots = new List<WoWPoint>();

			List<XElement> hotspots = element.Elements().ToList();
			foreach (XElement hotspot in hotspots)
			{
				try
				{
					if (hotspot.Name == "Hotspot")
					{
						try
						{
							float? x = null;
							float? y = null;
							float? z = null;

							XAttribute[] attributes = hotspot.Attributes().ToArray();

							foreach (XAttribute attribute in attributes)
							{
								try
								{
									switch (attribute.Name.ToString().ToLowerInvariant())
									{
										case "x":
											float tempX;
											if (!float.TryParse(attribute.Value, out tempX))
											{
												throw new ProfileAttributeExpectedException<float>(attribute);
											}

											x = tempX;
											break;
										case "y":
											float tempY;
											if (!float.TryParse(attribute.Value, out tempY))
											{
												throw new ProfileAttributeExpectedException<float>(attribute);
											}

											y = tempY;
											break;
										case "z":
											if (!x.HasValue || !y.HasValue)
												throw new ProfileException(StyxResources.You_have_placed_the__Z__attribute_before_the__X__and__Y__attribute_);

											float tempZ;
											if (!ProfileHelper.ParseHeightString(x.Value, y.Value, attribute.Value, out tempZ))
											{
												throw new ProfileAttributeExpectedException<float>(attribute);
											}

											z = tempZ;
											break;
										case "name":
											break;
										default:
											throw new ProfileUnknownAttributeException(attribute, "X", "Y", "Z", "Name");
									}
								}
								catch (ProfileException ex)
								{
									Logging.WriteException(ex);
								}
							}

							if (x.HasValue && y.HasValue && z.HasValue)
							{
								Hotspots.Add(new WoWPoint(x.Value, y.Value, z.Value));
							}
							else
							{
								throw new ProfileMissingAttributeException<float>(x.HasValue ? (y.HasValue ? "Z" : "Y") : "X", hotspot);
							}
						}
						catch (ProfileException ex)
						{
							Logging.WriteException(ex);
						}
					}
					else
					{
						throw new ProfileUnknownElementException(element, "Hotspot");
					}
				}
				catch (ProfileException ex)
				{
					Logging.WriteException(ex);
				}
			}

			_circledHotspots = new CircularQueue<WoWPoint>();
			Hotspots.ForEach(_circledHotspots.Enqueue);
			CycleToNearest();
		}

		public void CycleToNearest()
		{
			WoWPoint nearest = WoWPoint.Zero;
			float nearestDist = float.MaxValue;
			foreach (WoWPoint pnt in Hotspots)
			{
				float dist = pnt.Distance(StyxWoW.Me.Location);
				if (dist < nearestDist)
				{
					nearestDist = dist;
					nearest = pnt;
				}
			}

			if (nearest != WoWPoint.Zero)
			{
				_circledHotspots.CycleTo(nearest);
			}
		}

		public WoWPoint GetNextHotspot()
		{
			return _circledHotspots.Count > 0 ? _circledHotspots.Dequeue() : WoWPoint.Zero;
		}

		private readonly Random _rand = new Random();

		/// <summary>
		/// Gets a random hotspot that is not on the blacklist.
		/// </summary>
		/// <returns></returns>
		public WoWPoint GetRandomHotspot()
		{
			List<WoWPoint> candidates = Hotspots.Where(pnt => !Blacklist.ContainsKey(pnt)).ToList();
			return candidates.Count > 0 ? candidates[_rand.Next(0, candidates.Count)] : WoWPoint.Zero;
		}

		public void BlacklistPoint(WoWPoint pnt, TimeSpan forTime)
		{
			BlacklistPoint(pnt, DateTime.Now.Add(forTime));
		}

		public void BlacklistPoint(WoWPoint pnt, DateTime expiration)
		{
			if (!_blackList.ContainsKey(pnt))
			{
				_blackList.Add(pnt, expiration);
			}
			else
			{
				_blackList[pnt] = expiration;
			}
		}

		private void RefreshBlacklist()
		{
			DateTime now = DateTime.Now;
			foreach (var kvp in _blackList)
			{
				if (now > kvp.Value)
				{
					_blackList.Remove(kvp.Key);
				}
			}
		}

		private static WoWPoint _currentHotspot = WoWPoint.Zero;

		public static WoWPoint CurrentHotSpot
		{
			get
			{
				try
				{
					EnsureHotspots();
				}
				catch (UserException ex)
				{
					Logging.Write(ex.Message);
				}
				catch (Exception ex)
				{
					Logging.WriteException(ex);
				}

				return _currentHotspot;
			}
		}

		public static WoWPoint LastHotSpot { get; set; }

		public static Stopwatch Timer { get; set; }

		private static Profile _lastProfile;

		private static void EnsureHotspots()
		{
			if (ProfileManager.CurrentOuterProfile == null)
				throw new UserException(StyxResources.No_profile_is_loaded_);

			HotspotManager hotspots = ProfileManager.CurrentProfile.HotspotManager;

			if (hotspots == null)
			{
				Logging.Write(StyxResources.No_suitable_hotspots_in__0__, ProfileManager.CurrentProfile.Name);
				throw new UserException(StyxResources.No_hotspots_have_been_defined_);
			}

			if (ProfileManager.CurrentProfile != _lastProfile)
			{
				_lastProfile = ProfileManager.CurrentProfile;
				LastHotSpot = _currentHotspot;
				_currentHotspot = hotspots.GetNextHotspot();
				Timer.Reset();
			}

            if (Timer.Elapsed.TotalMinutes > 5 || (LastHotSpot == _currentHotspot && Targeting.Instance.TargetList.Count == 0))
			{
				LastHotSpot = _currentHotspot;
				_currentHotspot = hotspots.GetNextHotspot();
				Timer.Reset();
			}
		}
	}
}
