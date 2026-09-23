using System;
using System.Collections.Generic;
using System.Linq;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.Patchables;
using Styx.WoWInternals;
using Styx.WoWInternals.DBC;

namespace Bots.Professionbuddy
{
	public class TradeSkill
	{
		private Dictionary<uint, IngredientSubClass> _ingredients;
		private List<Tool> _tools;

		public static readonly List<SkillLine> SupportedSkills = new List<SkillLine>
		{
			SkillLine.Alchemy,
			SkillLine.Blacksmithing,
			SkillLine.Cooking,
			SkillLine.Enchanting,
			SkillLine.Engineering,
			SkillLine.FirstAid,
			SkillLine.Inscription,
			SkillLine.Jewelcrafting,
			SkillLine.Leatherworking,
			SkillLine.Mining,
			SkillLine.Tailoring
		};

		private TradeSkill(WoWSkill skill)
		{
			WoWSkill = skill;
			Recipes = new Dictionary<int, Recipe>();
		}

		public WoWSkill WoWSkill { get; internal set; }

		public uint Bonus
		{
			get { return WoWSkill.Bonus; }
		}

		public int Level
		{
			get { return WoWSkill.CurrentValue; }
		}

		public int MaxLevel
		{
			get { return WoWSkill.MaxValue; }
		}

		public string Name
		{
			get { return WoWSkill.Name; }
		}

		public SkillLine SkillLine
		{
			get { return (SkillLine)WoWSkill.Id; }
		}

		public Dictionary<uint, IngredientSubClass> Ingredients
		{
			get
			{
				if (_ingredients == null)
					InitIngredientList();
				return _ingredients;
			}
		}

		public List<Tool> Tools
		{
			get
			{
				if (_tools == null)
					InitToolList();
				return _tools;
			}
		}

		public Dictionary<int, Recipe> KnownRecipes
		{
			get { return Recipes.Where(kv => kv.Value.KnowsRecipe).ToDictionary(kv => kv.Key, kv => kv.Value); }
		}

		public Dictionary<int, Recipe> Recipes { get; private set; }

		public int RecipeCount
		{
			get { return KnownRecipes.Count; }
		}

		private void AddRecipe(Recipe recipe)
		{
			if (!Recipes.ContainsKey(recipe.SpellId))
			{
				Recipes.Add(recipe.SpellId, recipe);
				recipe.InitIngredients();
				recipe.InitTools();
			}
		}

		internal Tool GetTool(uint index, Tool.ToolType toolType)
		{
			Tool tool = Tools.FirstOrDefault(t => t.Id == index);
			if (tool == null)
			{
				tool = new Tool(index, toolType);
				Tools.Add(tool);
			}
			return tool;
		}

		private void InitIngredientList()
		{
			_ingredients = new Dictionary<uint, IngredientSubClass>();
			foreach (var kv in KnownRecipes)
				kv.Value.InitIngredients();
		}

		internal void InitToolList()
		{
			_tools = new List<Tool>();
			foreach (var kv in KnownRecipes)
				kv.Value.InitTools();
		}

		public void PulseBags()
		{
			if (!TreeRoot.IsRunning)
				ObjectManager.Update();
			foreach (var ingred in Ingredients)
				ingred.Value.UpdateInBagsCount();
			foreach (Tool tool in Tools)
				tool.UpdateToolStatus();
		}

		public void PulseSkill()
		{
			Update();
		}

		public void Update()
		{
			if (!StyxWoW.IsInGame)
				return;
			if (!TreeRoot.IsRunning)
				ObjectManager.Update();
			WoWSkill = StyxWoW.Me.GetSkill(SkillLine);
			Dictionary<int, Recipe> knownRecipes = KnownRecipes;
			foreach (var kv in Recipes)
				kv.Value.Update();
			using (StyxWoW.Memory.AcquireFrame())
			{
				IEnumerable<KeyValuePair<int, Recipe>> newRecipes = KnownRecipes.Except(knownRecipes);
				foreach (var kv in newRecipes)
				{
					PBLog.Log("Leaned a new recipe {0}", kv.Value.Name);
					kv.Value.UpdateHeader();
				}
			}
			PulseBags();
		}

		public static TradeSkill GetTradeSkill(SkillLine skillLine)
		{
			return GetTradeSkills(new[] { skillLine }).FirstOrDefault();
		}

		public static IEnumerable<TradeSkill> GetTradeSkills(SkillLine[] skillLines)
		{
			if (!StyxWoW.IsInGame)
				throw new InvalidOperationException("Must Be in game to call GetTradeSkill()");
			if (!TreeRoot.IsRunning)
				ObjectManager.Update();
			List<SkillLineAbility> abilities = SkillLineAbility.GetAbilities().ToList();
			foreach (SkillLine skillLine in skillLines)
			{
				if (skillLine == 0 || !SupportedSkills.Contains(skillLine))
					throw new InvalidOperationException(string.Format("The tradekill {0} can not be loaded", skillLine));
				TradeSkill tradeSkill = null;
				try
				{
					using (StyxWoW.Memory.AcquireFrame())
					{
						WoWSkill wowSkill = StyxWoW.Me.GetSkill(skillLine);
						tradeSkill = new TradeSkill(wowSkill);
						List<SkillLine> childSkillLines = (from row in StyxWoW.Db[ClientDb.SkillLine]
							select row.GetStruct<SkillLineInfo.SkillLineEntry>() into entry
							where entry.ID == (uint)skillLine || entry.ParentSkillLineId == (uint)skillLine
							select (SkillLine)entry.ID).ToList();
						IEnumerable<SkillLineAbility> skillAbilities = abilities.Where(a => childSkillLines.Contains(a.SkillLine));
						foreach (SkillLineAbility ability in skillAbilities)
						{
							if (ability.NextSpellId == 0 && ability.GreySkillLevel > 0 && ability.TradeSkillCategoryId != 0 && ability.SpellId != 0)
							{
								WoWSpell spell = WoWSpell.FromId(ability.SpellId);
								if (spell != null)
								{
									var recipe = new Recipe(tradeSkill, ability, spell);
									recipe.UpdateHeader();
									tradeSkill.AddRecipe(recipe);
								}
							}
						}
					}
				}
				catch (Exception ex)
				{
					Logging.WriteException(ex);
				}
				yield return tradeSkill;
			}
		}
	}
}
