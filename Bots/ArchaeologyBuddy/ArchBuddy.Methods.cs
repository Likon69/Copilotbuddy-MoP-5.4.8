using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Media;
using ArchaeologyBuddy.Resources;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.AreaManagement;

using Styx.Patchables;
using Styx.Pathing;
using Styx.WoWInternals;
using System.Diagnostics;
using Styx.WoWInternals.World;
using System.Globalization;
using System.Xml.Linq;
using Vector2 = Tripper.Tools.Math.Vector2;

namespace Bots.ArchaeologyBuddy
{
	public partial class ArchBuddy
	{
		#region Log Methods

		private static void Log(Color color, string format, params object[] args)
		{
			Logging.Write(color, string.Format("[{0}]: ", ArchaeologyResources.ArchBuddy) + format, args);
		}

		internal static void Log(string format, params object[] args)
		{
            Log(Colors.LightGray, format, args);
		}

		private static void dLog(string format, params object[] args)
		{
			Logging.WriteDiagnostic(Colors.Red, string.Format("[{0}]: ", ArchaeologyResources.ArchBuddy) + format, args);
		}

		#endregion

		private static WoWPoint FindHighestZPoint()
		{
			return FindHighestZPoint(StyxWoW.Me.X, StyxWoW.Me.Y);
		}

		private static WoWPoint FindHighestZPoint(float x, float y)
		{
			WoWPoint result = WoWPoint.Empty;

			List<float> zValues = Navigator.FindHeights(x, y);

			if (zValues != null && zValues.Count > 0)
			{
				result = new WoWPoint(x, y, zValues.Max());
			}

			return result;
		}

		private static WoWPoint MapToWorldCoords(float X, float Y, uint mapId)
		{
			Random _random = new Random();
			WoWPoint worldPoint = new WoWPoint();
			WoWDb.DbTable worldMapArea = StyxWoW.Db[ClientDb.WorldMapArea];
			WoWDb.Row worldMapAreaFields = worldMapArea.GetRow(mapId);
			float ay = worldMapAreaFields.GetField<float>(4);
			float by = worldMapAreaFields.GetField<float>(5);
			float ax = worldMapAreaFields.GetField<float>(6);
			float bx = worldMapAreaFields.GetField<float>(7);
			worldPoint.X = ax + (Y * (bx - ax));
			worldPoint.Y = ay + (X * (by - ay));
			float x = worldPoint.X;
			float y = worldPoint.Y;
			float z = float.MaxValue;
			int tries = 0;
			Stopwatch pulseTimer = new Stopwatch();
			pulseTimer.Start();
			while (z == float.MaxValue)
			{
				if (pulseTimer.ElapsedMilliseconds >= 2000)
				{
					ObjectManager.Update();
					Pulsator.Pulse(BotManager.Current.PulseFlags);
					pulseTimer.Reset();
				}

				if (tries > 20)
				{
					return WoWPoint.Empty;
				}
				tries++;

				List<float> zValues = Navigator.FindHeights(x, y);

				if (zValues == null || zValues.Count == 0)
				{
					x += (float)_random.NextDouble() - 5f;
					y += (float)_random.NextDouble() - 5f;
				}
				else
				{
					z = zValues.Max();
					break;
				}
			}
			worldPoint.Z = z;
			return worldPoint;
		}

		internal static float PathLength(WoWPoint destPoint)
		{
			WoWPoint[] path = Navigator.GeneratePath(StyxWoW.Me.Location, destPoint);
			if (path.Length == 0)
				return float.MaxValue;
			
			float totalLength = StyxWoW.Me.Location.Distance(path[0]);

			if (path.Length > 1)
			{
				for (int i = 1; i < path.Length; i++)
				{
					totalLength += path[i].Distance(path[i - 1]);
				}
			}

			return totalLength;
		}

		private static List<Vector2> GetPOIBounds(int digsiteId)
		{
            //throw new NotImplementedException("Need to figure out where QuestPOIPoint.dbc is used. IDA says not at all.");
            List<Vector2> list = new List<Vector2>();
            WoWDb.DbTable table = StyxWoW.Db[ClientDb.QuestPOIPoint];

            for (int j = table.MinIndex; j <= table.MaxIndex; j += 2)
            {
                WoWDb.Row row = table.GetRow((uint)j);

                if (!row.IsValid)
                    continue;

                int blobId = row.GetField<int>(3);
                if (blobId == digsiteId)
                {
                    Vector2 v = new Vector2(row.GetField<int>(1), row.GetField<int>(2));
                    list.Add(v);
                }
            }
            return list;
		}

		private static void ResetVariables()
		{
			CurrentDigsite = null;
			nextSurveyPoint = WoWPoint.Empty;
			IsInDigSite = false;
			ClosestDigSiteName = "";
			ClosestDigSiteID = 0;
			ClosestDigSiteLocation = WoWPoint.Empty;
			digSiteCheck = false;
			collectedArtifact = 0;
			ValidDigSitePoint = WoWPoint.Empty;
			myLastLocation = WoWPoint.Empty;
			safeMyLastLocation = WoWPoint.Empty;
		}

		private static bool AllPointsInLoS(WoWPoint[] path)
		{
			for (int i = 0; i < path.Length - 1; i++)
			{
				if (!GameWorld.IsInLineOfSight(path[i], path[i + 1]))
					return false;
			}
			return true;
		}

		private static WoWPoint WoWPointFromString(string str)
		{
			string[] split = str.Split(", ".ToArray(), StringSplitOptions.RemoveEmptyEntries);

            float x = float.Parse(split[0], CultureInfo.InvariantCulture);
            float y = float.Parse(split[1], CultureInfo.InvariantCulture);
            float z = float.Parse(split[2], CultureInfo.InvariantCulture);

			return new WoWPoint(x,y,z);
		}

        internal static Dictionary<Digsite, DateTime> TemporaryBlacklist = new Dictionary<Digsite, DateTime>();

        internal static void TempBlacklist(Digsite site, TimeSpan length)
        {
            if (TemporaryBlacklist.ContainsKey(site))
                return;

            TemporaryBlacklist.Add(site, DateTime.Now.Add(length));
        }

        internal static bool IsBlacklisted(Digsite site)
        {
            if (TemporaryBlacklist.ContainsKey(site))
            {
                var end = TemporaryBlacklist[site];
                if (end < DateTime.Now)
                    TemporaryBlacklist.Remove(site);
                else
                    return true;
            }
            return PermanentBlacklist.Contains(site);
        }

		internal static void SaveBlacklist()
		{
			XDocument xmlFile = new XDocument(
									new XComment("This xml document is generated by ArchaeologyBuddy."),
									new XComment("DO NOT EDIT THIS FILE. USE SETTINGS FORM INSTEAD"),
									new XElement("AB",
										from i in PermanentBlacklist
										select new XElement("digsite",
											new XAttribute("ID", i.ID),
											new XAttribute("Name", i.Name))));

            xmlFile.Save(GatherbuddyAvoidListPath);
		}

		internal static void LoadBlacklist()
		{
            string filename = GatherbuddyAvoidListPath;
			PermanentBlacklist = (from e in XElement.Load(filename).Elements("digsite") select Digsite.FromId(int.Parse(e.Attribute("ID").Value))).ToList();
		}
	}
}
