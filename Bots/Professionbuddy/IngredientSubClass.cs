namespace Bots.Professionbuddy
{
	public class IngredientSubClass
	{
		internal Ingredient Parent;
		private string _name;

		internal IngredientSubClass(Ingredient parent, uint inBagCount)
		{
			Parent = parent;
			InBagsCount = inBagCount;
		}

		public string Name
		{
			get { return _name ?? (_name = Util.GetItemCacheName(Parent.ID)); }
		}

		public uint InBagsCount { get; internal set; }

		internal void UpdateInBagsCount()
		{
			InBagsCount = Ingredient.GetInBagItemCount(Parent.ID);
		}
	}
}
