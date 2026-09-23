using System;
using System.Linq;
using Styx;
using Styx.Patchables;
using Styx.WoWInternals;
using Styx.WoWInternals.DBC;
using Styx.WoWInternals.WoWObjects;

namespace Bots.Professionbuddy
{
	public class Tool : IEquatable<Tool>
	{
		private readonly ToolType _toolType;
		private string _name;

		internal Tool(uint id, ToolType toolType)
		{
			Id = id;
			_toolType = toolType;
			UpdateToolStatus();
			ItemId = toolType == ToolType.Item ? id : 0;
		}

		public uint Id { get; private set; }

		public uint ItemId { get; private set; }

		public bool HasTool { get; internal set; }

		public string Name
		{
			get { return _name ?? (_name = GetName()); }
		}

		private string GetName()
		{
			string name = null;
			switch (_toolType)
			{
				case ToolType.SpellFocus:
				{
					WoWDb.DbTable t = StyxWoW.Db[ClientDb.SpellFocusObject];
					WoWDb.Row r = t.GetRow(Id);
					name = r.GetStringField(1);
					break;
				}
				case ToolType.AreaTable:
					name = AreaTable.FromId(Id).AreaName;
					break;
				case ToolType.Item:
					name = Util.GetItemCacheName(Id);
					break;
				case ToolType.Totem:
				{
					WoWDb.DbTable t = StyxWoW.Db[ClientDb.TotemCategory];
					WoWDb.Row r = t.GetRow(Id);
					name = r.GetStringField(1);
					break;
				}
			}
			return name;
		}

		internal void UpdateToolStatus()
		{
			switch (_toolType)
			{
				case ToolType.SpellFocus:
				case ToolType.AreaTable:
					HasTool = true;
					break;
				case ToolType.Item:
				case ToolType.Totem:
					if (ItemId != 0)
					{
						HasTool = StyxWoW.Me.BagItems.Any(i => i.Entry == ItemId);
						break;
					}
					WoWItem item = StyxWoW.Me.BagItems.FirstOrDefault(i => i.Name == Name);
					if (item != null)
					{
						ItemId = item.Entry;
						HasTool = true;
					}
					else
					{
						HasTool = false;
					}
					break;
			}
		}

		public bool Equals(Tool other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return _toolType == other._toolType && Id == other.Id;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != GetType()) return false;
			return Equals((Tool)obj);
		}

		public override int GetHashCode()
		{
			return ((int)_toolType * 397) ^ (int)Id;
		}

		public static bool operator ==(Tool left, Tool right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(Tool left, Tool right)
		{
			return !Equals(left, right);
		}

		internal enum ToolType
		{
			SpellFocus,
			AreaTable,
			Item,
			Totem
		}
	}
}
