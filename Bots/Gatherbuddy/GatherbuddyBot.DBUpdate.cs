using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Styx;
using Styx.CommonBot;

using Styx.WoWInternals;
using System.IO;
using System.Net;
using Styx.WoWInternals.WoWObjects;

namespace Bots.Gatherbuddy
{
	public partial class GatherbuddyBot : BotBase
	{

		//private static string s = "!@#$";
		private static void DBUpdate(WoWGameObject obj, bool badNode)
		{
			return;
			//string Entry = obj.Entry.ToString();
			//string Name = obj.Name.ToString();
			//string Location = obj.Location.ToString();
			//string ZoneID = StyxWoW.Me.ZoneId.ToString();
			//string ContinentID = Lua.GetReturnVal<string>("return GetCurrentMapContinent()", 0);
			//string Type = obj.IsHerb ? "Herb" : "Mine";
			//string BadNode = badNode ? "1" : "0";
			//string data = Entry + s + Name + s + Location + s + ZoneID + s + ContinentID + s + Type + s + BadNode + s;
			//string result = SendAndReceiveData(Url, "update", data);
		}

		private static string Url = @"http://redacted.invalid";
		private static string SendAndReceiveData(string url, string api, string data)
		{
			var request =
				HttpWebRequest.Create(Url) as HttpWebRequest;

			request.ContentType = "application/x-www-form-urlencoded";
			request.Method = "POST";
			request.Timeout = 2000;
			byte[] dataBytes = Encoding.UTF8.GetBytes(BuildData(api, data));

			Stream stream = null;
			try
			{
				request.ContentLength = dataBytes.Length;
				stream = request.GetRequestStream();
				stream.Write(dataBytes, 0, dataBytes.Length);
			}
			catch { }
			finally
			{
				if (stream != null)
					stream.Close();
			}

			string responseString = null;
			var postResponse = default(WebResponse);

			try
			{ // get the response
				postResponse = request.GetResponse();
				StreamReader responseReader = new StreamReader(postResponse.GetResponseStream());
				responseString = responseReader.ReadToEnd().Trim();
			}
			catch { }
			finally
			{
				if (postResponse != null)
					postResponse.Close();
			}
			return responseString;
		}

		private static string POST_UPDATE_BASE_METHOD = "t={0}&data={1}";
		private static string BuildData(string api, string data)
		{
			var UrlSb = new StringBuilder();

			UrlSb.AppendFormat(
				POST_UPDATE_BASE_METHOD,
				api,
				data);

			return UrlSb.ToString();
		}
	}
}
