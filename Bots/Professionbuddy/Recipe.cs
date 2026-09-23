using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Styx;
using Styx.Patchables;
using Styx.WoWInternals;
using Styx.WoWInternals.DBC;
using Styx.WoWInternals.WoWCache;

namespace Bots.Professionbuddy
{
	public class Recipe
	{
		private readonly TradeSkill _parent;
		private readonly SkillLineAbility _skillLineAbility;
		private string _header;
		private List<Ingredient> _ingredients;
		private List<Tool> _tools;

		internal Recipe(TradeSkill parent, SkillLineAbility skillLineAbility, WoWSpell spell)
		{
			_parent = parent;
			_skillLineAbility = skillLineAbility;
			Spell = spell;
			Update();
		}

		public uint CanRepeatNum
		{
			get
			{
				uint ret = uint.MaxValue;
				foreach (Ingredient ingred in Ingredients)
				{
					var cnt = (uint)Math.Floor(ingred.InBagItemCount / (double)ingred.Required);
					if (ret > cnt)
						ret = cnt;
				}
				return ret;
			}
		}

		public uint CanRepeatNum2
		{
			get
			{
				uint ret = uint.MaxValue;
				foreach (Ingredient ingred in Ingredients)
				{
					var cnt = (uint)Math.Floor(Ingredient.GetInBagItemCount(ingred.ID) / (double)ingred.Required);
					if (ret > cnt)
						ret = cnt;
				}
				return ret;
			}
		}

		public uint CraftedItemId
		{
			get { return Spell.SpellEffect1.ItemType; }
		}

		public int DisplayOrder
		{
			get { return _skillLineAbility.DisplayOrder; }
		}

		public RecipeDifficulty Difficulty
		{
			get
			{
				int skillLevel = _parent.WoWSkill.CurrentValue;
				if (skillLevel < _skillLineAbility.YellowSkillLevel)
					return RecipeDifficulty.Optimal;
				if (skillLevel < _skillLineAbility.GreenSkillLevel)
					return RecipeDifficulty.Medium;
				if (skillLevel < _skillLineAbility.GreySkillLevel)
					return RecipeDifficulty.Easy;
				return RecipeDifficulty.Trivial;
			}
		}

		public bool KnowsRecipe { get; private set; }

		public int GreySkillLevel
		{
			get { return _skillLineAbility.GreySkillLevel; }
		}

		public int GreenSkillLevel
		{
			get { return (_skillLineAbility.YellowSkillLevel + _skillLineAbility.GreySkillLevel) / 2; }
		}

		public string Header
		{
			get
			{
				if (_header == null || string.IsNullOrEmpty(_header))
				{
					using (StyxWoW.Memory.AcquireFrame())
					{
						foreach (var kv in _parent.KnownRecipes)
						{
							UpdateHeader();
						}
					}
				}
				return _header;
			}
		}

		public ReadOnlyCollection<Ingredient> Ingredients
		{
			get
			{
				if (_ingredients == null)
					InitIngredients();
				return _ingredients != null ? _ingredients.AsReadOnly() : null;
			}
		}

		public string Name
		{
			get { return Spell.Name; }
		}

		public int OrangeSkillLevel
		{
			get { return _skillLineAbility.OrangeSkillLevel; }
		}

		public int OptimalSkillups
		{
			get { return _skillLineAbility.SkillPointsEarned; }
		}

		public int SpellId
		{
			get { return _skillLineAbility.SpellId; }
		}

		public SkillLine Skill
		{
			get { return _skillLineAbility.SkillLine; }
		}

		public int Skillups
		{
			get
			{
				switch (Difficulty)
				{
					case RecipeDifficulty.Optimal:
						return OptimalSkillups;
					case RecipeDifficulty.Medium:
					case RecipeDifficulty.Easy:
						return 1;
					default:
						return 0;
				}
			}
		}

		public WoWSpell Spell { get; private set; }

		public ReadOnlyCollection<Tool> Tools
		{
			get
			{
				if (_tools == null)
					InitTools();
				return _tools != null ? _tools.AsReadOnly() : null;
			}
		}

		public int YellowSkillLevel
		{
			get { return _skillLineAbility.YellowSkillLevel; }
		}

		internal void InitIngredients()
		{
			if (_ingredients != null)
				return;
			WoWSpell.SpellReagentsEntry reagents = Spell.InternalInfo.SpellReagents;
			if (reagents.Reagent == null)
				return;
			_ingredients = new List<Ingredient>();
			for (int i = 0; i < reagents.Reagent.Length; i++)
			{
				if (reagents.Reagent[i] != 0)
					_ingredients.Add(new Ingredient((uint)reagents.Reagent[i], reagents.ReagentCount[i], _parent.Ingredients));
			}
		}

		public void UpdateHeader()
		{
			if (!string.IsNullOrEmpty(_header))
				return;
			if (CraftedItemId == 0)
			{
				_header = SkillLineInfo.FromId((uint)Skill).Verb;
				return;
			}
			WoWCache.InfoBlock cache = StyxWoW.Cache[CacheDb.ItemClass].GetInfoBlockById(CraftedItemId);
			if (cache == null)
				return;
			WoWDb.DbTable t = StyxWoW.Db[ClientDb.ItemSubClass];
			for (int i = t.MinIndex; i <= t.MaxIndex; i++)
			{
				WoWDb.Row row = t.GetRow((uint)i);
				if (row != null && row.IsValid)
				{
					int classId = row.GetField<int>(1);
					int subClassId = row.GetField<int>(2);
					if (classId == cache.Item.ClassId && subClassId == cache.Item.SubClassId)
					{
						_header = row.GetStringField(12);
						if (string.IsNullOrEmpty(_header))
							_header = row.GetStringField(11);
						return;
					}
				}
			}
		}

		internal void InitTools()
		{
			if (_tools != null)
				return;
			_tools = new List<Tool>();
			if (Spell.InternalInfo.SpellCastingRequirementsId != 0)
			{
				WoWSpell.SpellCastingRequirementsEntry castingReqs = Spell.InternalInfo.SpellCastingRequirements;
				if (castingReqs.RequiresSpellFocus != 0)
					_tools.Add(_parent.GetTool(castingReqs.RequiresSpellFocus, Tool.ToolType.SpellFocus));
				if (castingReqs.AreaGroupId != 0)
					_tools.Add(_parent.GetTool((uint)castingReqs.AreaGroupId, Tool.ToolType.SpellFocus));
			}
			if (Spell.InternalInfo.SpellTotemsId != 0)
			{
				uint totem = Spell.InternalInfo.SpellTotems.Totem.FirstOrDefault(t => t != 0);
				uint totemCategory = Spell.InternalInfo.SpellTotems.TotemCategory.FirstOrDefault(t => t != 0);
				if (totem != 0)
					_tools.Add(_parent.GetTool(totem, Tool.ToolType.Item));
				if (totemCategory != 0)
					_tools.Add(_parent.GetTool(totemCategory, Tool.ToolType.Totem));
			}
		}

		public void Update()
		{
			KnowsRecipe = StyxWoW.Me.KnowsSpell(SpellId);
		}
	}
}
