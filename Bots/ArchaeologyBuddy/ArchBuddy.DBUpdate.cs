using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Styx;
using Styx.CommonBot;

using Styx.Pathing;
using Styx.WoWInternals;
using System.IO;
using System.Net;
using System.Collections.Specialized;
using Styx.WoWInternals.WoWObjects;

namespace Bots.ArchaeologyBuddy
{
	public partial class ArchBuddy : BotBase
	{
		private static void DBUpdate(WoWGameObject gameObject)
		{
            string locale = Lua.GetReturnVal<string>("return GetLocale()", 0);

            if (locale != null && locale != "enGB" && locale != "enUS")
                return;

            string ID = ClosestDigSiteID.ToString();
            string Name = ClosestDigSiteName.Replace("'","");
            string Location = gameObject.Location.ToString().Replace("<","").Replace(">","");
            string ZoneID = StyxWoW.Me.ZoneId.ToString();
            string ContinentID = StyxWoW.Me.MapId.ToString();
            string OutDoors = gameObject.IsOutdoors ? "1" : "0";
            string HigherZExists = Navigator.FindHeights(gameObject.X, gameObject.Y).Any(p => p > gameObject.Z + 3) ? "1" : "0";
            string Type = "";
            switch (gameObject.Entry)
            {
                case 207188:
                    Type = "Draenei";
                    break;

                case 204282:
                    Type = "Dwarf";
                    break;

                case 206836:
                    Type = "Fossil";
                    break;

                case 203078:
                    Type = "Nerubian";
                    break;

                case 203071:
                    Type = "NightElf";
                    break;

                case 207187:
                    Type = "Orc";
                    break;

                case 207190:
                    Type = "Tolvir";
                    break;

                case 202655:
                    Type = "Troll";
                    break;

                case 207189:
                    Type = "Vrykul";
                    break;

                case 211163:
                    Type = "Pandaren";
                    break;
                  
                case 211174:
                    Type = "Mogu";
                    break;

                case 218950:
                    Type = "Mantid";
                    break;

                default:
                    Type = "Unknown";
                    break;

            }

            var postParams = new NameValueCollection
            {
                { "t", "update" },
                { "id", ID },
                { "name", Name },
                { "location", Location },
                { "zoneId", ZoneID },
                { "continentId", ContinentID },
                { "outDoors", OutDoors },
                { "higherZExists", HigherZExists },
                { "type", Type }
            };

            SendAndReceiveData(Url, postParams);
		}

		private static string Url = @"http://redacted.invalid";
        internal static void SendAndReceiveData(string url, NameValueCollection postparams)
        {
            try
            {
                WebClient client = new WebClient {Encoding = Encoding.UTF8};
                client.UploadValuesAsync(new Uri(url), "POST", postparams);
            }
            catch { }
        }
	}
}
