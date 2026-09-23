using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bots.DungeonBuddy.Enums;
using Bots.DungeonBuddy.Helpers;
using Bots.DungeonBuddy.Profiles;
using Bots.DungeonBuddy.Profiles.Handlers;
using Styx;
using Styx.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Tripper.Tools.Math;

namespace Bots.DungeonBuddy;

internal static class DungeonBuddyExtensions
{
	public static bool HasFlag(this WoWPartyMember.GroupRole @this, WoWPartyMember.GroupRole flag)
	{
		return (@this & flag) != 0;
	}

	/// <summary>
	///     Returns true if Boss has no location or path associated with it
	/// </summary>
	/// <param name="boss"></param>
	/// <returns></returns>
	public static bool IsRandomSpawnBoss(this Boss boss)
	{
		return boss != null && boss.PathBreadCrumbs.Peek() == WoWPoint.Zero;
	}

	/// <summary>
	///     Returns true if the boss is not optional or if optional and 'Kill Optional Bosses' is setting is true.
	/// </summary>
	/// <param name="boss"></param>
	/// <returns>true if boss should be killed</returns>
	public static bool ShouldKillBoss(this Boss boss)
	{
		return boss != null && ((boss.Optional && DungeonBuddySettings.Instance.KillOptionalBosses) || !boss.Optional);
	}

	/// <summary>
	///     Returns the Boss object of a WoWunit
	/// </summary>
	/// <param name="unit"></param>
	/// <returns>Boss object</returns>
	public static Boss ToBoss(this WoWUnit unit)
	{
		return unit != null && ProfileManager.CurrentProfile != null ? ProfileManager.CurrentProfile.BossEncounters.FirstOrDefault(b => b.Entry == unit.Entry) : null;
	}

	/// <summary>
	///     Returns true if the unit is a boss
	/// </summary>
	/// <param name="unit"></param>
	/// <returns>true if unit is a boss</returns>
	public static bool IsBoss(this WoWUnit unit)
	{
		return unit != null && ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.BossEncounters.Any(b => b.Entry == unit.Entry);
	}

	/// <summary>
	///     Returns the current dungeon that the player is in
	/// </summary>
	/// <param name="me"></param>
	/// <returns>Current dungeon</returns>
	public static LfgDungeons CurrentLfgDungeon(this LocalPlayer me)
	{
		return LfgDungeons.FromId(StyxWoW.Me.GroupInfo.LfgDungeonId);
	}

	/// <summary>
	///     My bags I have on me.
	/// </summary>
	/// <param name="me"></param>
	/// <returns></returns>
	public static IEnumerable<WoWBag> MyCarriedBags(this LocalPlayer me)
	{
		var backpack = StyxWoW.Me.Inventory.Backpack;
		if (backpack != null)
			yield return backpack;
		for (uint i = 0; i < 4; i++)
		{
			var bag = me.GetBagAtIndex(i);
			if (bag != null)
				yield return bag;
		}
	}

	/// <summary>
	/// Checks if item has a given spell.
	/// </summary>
	/// <param name="item"></param>
	/// <param name="spellName"></param>
	/// <returns></returns>
	public static bool HasSpell(this WoWItem item, string spellName)
	{
		try
		{
			return item.ItemSpells != null && item.ItemSpells.Any(
				s => s != null && s.ActualSpell != null && s.ActualSpell.Name.Equals(spellName, StringComparison.InvariantCultureIgnoreCase));
		}
		catch (NullReferenceException)
		{
			return false;
		}
	}

	public static float PathDistance(this WoWObject unit)
	{
		float distance = 0;
		if (unit != null && unit.IsValid)
		{
			var path = Navigator.GeneratePath(StyxWoW.Me.Location, unit.Location);
			if (path != null && path.Length > 0)
			{
				for (int n1 = 0, n2 = 1; n2 < path.Length; n1++, n2++)
				{
					distance += path[n1].Distance(path[n2]);
				}
			}
		}
		return distance;
	}

	public static WoWPoint SetToNearestZ(this WoWPoint point)
	{
		try
		{
			var zlist = Navigator.FindHeights(point.X, point.Y);
			if (zlist != null && zlist.Count > 0)
			{
				point.Z = zlist.OrderBy(z => Math.Abs(z - point.Z)).FirstOrDefault();
			}
		}
		catch { }
		return point;
	}

	public static float Distance2DSqr(this Vector3 from, Vector3 to)
	{
		float num = from.X - to.X;
		float num2 = from.Y - to.Y;
		return num * num + num2 * num2;
	}

	public static float Distance2D(this Vector3 from, Vector3 to)
	{
		float num = from.X - to.X;
		float num2 = from.Y - to.Y;
		return (float)Math.Sqrt(num * num + num2 * num2);
	}

	public static Vector3 RayCast(this Vector3 from, float headingRadians, float distance)
	{
		return new Vector3(from.X + (float)Math.Cos(-headingRadians) * distance, from.Y + (float)Math.Sin(headingRadians) * distance, from.Z);
	}

	/// <summary>
	///     Converts a string to a formated UTF-8 string using \ddd format where ddd is a 3 digit number. Useful when importing names into lua that are UTF-16 or higher.
	/// </summary>
	/// <param name="text"></param>
	/// <returns></returns>
	public static string ToFormatedUTF8(this string text)
	{
		var buffer = new StringBuilder(Encoding.UTF8.GetByteCount(text));
		byte[] utf8Encoded = Encoding.UTF8.GetBytes(text);
		foreach (byte b in utf8Encoded)
		{
			buffer.Append($"\\{b:D3}");
		}
		return buffer.ToString();
	}

	public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T> source)
	{
		return source ?? Enumerable.Empty<T>();
	}
}
