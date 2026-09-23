using System;
using System.Collections.Generic;
using System.Linq;
using Styx;
using Styx.Common;

namespace Bots.Professionbuddy
{
	public class Ingredient
	{
		private readonly IngredientSubClass _subClass;
		internal Dictionary<uint, IngredientSubClass> MasterList;

		internal Ingredient(uint id, uint requiredNum, Dictionary<uint, IngredientSubClass> masterList)
		{
			ID = id;
			Required = requiredNum;
			MasterList = masterList;
			if (masterList.ContainsKey(id))
			{
				_subClass = masterList[id];
			}
			else
			{
				_subClass = new IngredientSubClass(this, GetInBagItemCount(id));
				masterList.Add(id, _subClass);
			}
		}

		public string Name
		{
			get { return _subClass.Name; }
		}

		public uint Required { get; private set; }

		public uint ID { get; private set; }

		public uint InBagItemCount
		{
			get { return _subClass.InBagsCount; }
		}

		public static uint GetInBagItemCount(uint id)
		{
			try
			{
				return (uint)StyxWoW.Me.BagItems.Sum(i => i != null && i.IsValid && i.Entry == id ? i.StackCount : 0);
			}
			catch (Exception ex)
			{
				Logging.WriteException(ex);
				return 0;
			}
		}
	}
}
