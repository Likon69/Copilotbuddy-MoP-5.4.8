using System.Collections.Generic;

namespace Bots.Quest.Objectives
{
	public static class DropDatabase
	{
		private static readonly Dictionary<uint, HashSet<uint>> _mobs;
		private static readonly Dictionary<uint, HashSet<uint>> _gameObjects;
        private static readonly Dictionary<uint, HashSet<uint>> _vendors;

		static DropDatabase()
        {
			_mobs = new Dictionary<uint, HashSet<uint>>();
			_gameObjects = new Dictionary<uint, HashSet<uint>>();
			_vendors = new Dictionary<uint, HashSet<uint>>();

			// Use regex "(?<=/npc=)\d+" to extract all mobs from an item

			#region Encrypted Twilight Text

			AddMobDrop(11882, 20404); // Twilight Stonecaller
			AddMobDrop(11880, 20404); // Twilight Avenger
			AddMobDrop(11881, 20404); // Twilight Geolord
			AddMobDrop(15201, 20404); // Twilight Flamereaver
			AddMobDrop(15542, 20404); // Twilight Marauder
			AddMobDrop(15213, 20404); // Twilight Overlord
			AddMobDrop(11883, 20404); // Twilight Master
			AddMobDrop(14479, 20404); // Twilight Lord Everun

			#endregion

			#region Crag Boar Rib

			AddMobDrop(1125, 2886); // Crag Boar
			AddMobDrop(1126, 2886); // Large Crag Boar
			AddMobDrop(1127, 2886); // Elder Crag Boar
			AddMobDrop(1689, 2886); // Scarred Crag Boar

			#endregion

			#region Chunk of Boar Meat

            AddMobDrop(1126, 769);  // Large Crag Boar
            AddMobDrop(1125, 769);  // Crag Boar
            AddMobDrop(1127, 769);  // Elder Crag Boar
            AddMobDrop(1689, 769);  // Scarred Crag Boar
            AddMobDrop(708, 769);   // Small Crag Boar
            AddMobDrop(3099, 769);  // Dire Mottled Boar
            AddMobDrop(3100, 769);  // Elder Mottled Boar
            AddMobDrop(3225, 769);  // Corrupted Mottled Boar
            AddMobDrop(3098, 769);  // Mottled Boar
            AddMobDrop(119, 769);   // Longsnout
            AddMobDrop(524, 769);   // Rockhide Boar
            AddMobDrop(113, 769);   // Stonetusk Boar
            AddMobDrop(390, 769);   // Porcine Entourage
            AddMobDrop(330, 769);   // Princess
            AddMobDrop(1191, 769);  // Mangy Mountain Boar
            AddMobDrop(1190, 769);  // Mountain Boar
            AddMobDrop(1192, 769);  // Elder Mountain Boar
            AddMobDrop(547, 769);   // Great Goretusk
            AddMobDrop(1985, 769);  // Thistle Boar
            AddMobDrop(454, 769);   // Young Goretusk
            AddMobDrop(157, 769);   // Goretusk

			#endregion

            #region Moongraze Stag Tenderloin

            AddMobDrop(17200, 23676);  // Moongraze Stag
            AddMobDrop(17201, 23676);  // Moongraze Buck

            #endregion

			#region Boar Intestines

			AddMobDrop(1190, 3172); // Mountain Boar
			AddMobDrop(1191, 3172); // Mangy Mountain Boar
			AddMobDrop(1192, 3172); // Elder Mountain Boar
			AddMobDrop(547, 3172); // Great Goretusk
			AddMobDrop(157, 3172); // Goretusk
			AddMobDrop(454, 3172); // Young Goretusk
			AddMobDrop(345, 3172); // Bellygrub

			#endregion

			#region Bear Meat

			AddMobDrop(2163, 3173);
			AddMobDrop(2164, 3173);
			AddMobDrop(1188, 3173);
			AddMobDrop(1189, 3173);
			AddMobDrop(1797, 3173);
			AddMobDrop(6788, 3173);
			AddMobDrop(1186, 3173);
			AddMobDrop(1778, 3173);
			AddMobDrop(2165, 3173);
			AddMobDrop(17347, 3173);
			AddMobDrop(1225, 3173);
			AddMobDrop(17348, 3173);
			AddMobDrop(12432, 3173);
			AddMobDrop(1961, 3173);
			AddMobDrop(1130, 3173);
			AddMobDrop(17661, 3173);
			AddMobDrop(3809, 3173);
			AddMobDrop(2356, 3173);
			AddMobDrop(2354, 3173);
			AddMobDrop(1128, 3173);
			AddMobDrop(822, 3173);


			#endregion

			#region Spider Ichor

			AddMobDrop(3821, 3174);
			AddMobDrop(4264, 3174);
			AddMobDrop(574, 3174);
			AddMobDrop(949, 3174);
			AddMobDrop(3820, 3174);
			AddMobDrop(930, 3174);
			AddMobDrop(4005, 3174);
			AddMobDrop(505, 3174);
			AddMobDrop(4006, 3174);
			AddMobDrop(4007, 3174);
			AddMobDrop(3819, 3174);
			AddMobDrop(4040, 3174);
			AddMobDrop(569, 3174);
			AddMobDrop(1111, 3174);
			AddMobDrop(539, 3174);
			AddMobDrop(442, 3174);
			AddMobDrop(217, 3174);
			AddMobDrop(1112, 3174);
			AddMobDrop(4263, 3174);
			AddMobDrop(1185, 3174);
			AddMobDrop(616, 3174);
			AddMobDrop(11921, 3174);
			AddMobDrop(1184, 3174);
			AddMobDrop(1195, 3174);
			AddMobDrop(1781, 3174);
			AddMobDrop(14266, 3174);
			AddMobDrop(1780, 3174);
			AddMobDrop(12433, 3174);
			AddMobDrop(471, 3174);

			#endregion

			#region Discolored Worg Heart

			AddMobDrop(1766, 3164);
			AddMobDrop(1765, 3164);

			#endregion

			#region Goretusk Liver

			AddMobDrop(345, 723); // Bellygrub
			AddMobDrop(547, 723); // Great Goretusk
			AddMobDrop(157, 723); // Goretusk
			AddMobDrop(454, 723); // Young Goretusk

			#endregion

			#region Turtle Meat

			AddMobDrop(2408, 3712);
			AddMobDrop(6369, 3712);
			AddMobDrop(2505, 3712);
			AddMobDrop(14123, 3712);
			AddMobDrop(5431, 3712);
			AddMobDrop(6352, 3712);
			AddMobDrop(13599, 3712);
			AddMobDrop(7977, 3712);
			AddMobDrop(13896, 3712);
			AddMobDrop(4397, 3712);
			AddMobDrop(4143, 3712);
			AddMobDrop(4144, 3712);
			AddMobDrop(8213, 3712);
			AddMobDrop(4142, 3712);
			AddMobDrop(4825, 3712);
			AddMobDrop(4824, 3712);
			AddMobDrop(14223, 3712);
			AddMobDrop(4887, 3712);

			#endregion

			#region Mature Spore Sac

			AddGameObjectDrop(182069, 24290); // Mature Spore Sac

			#endregion

			#region Bloodmaul Brutebane Brew

			AddGameObjectDrop(184504, 29443);

			#endregion

			#region Charred Razormane Wand

			AddMobDrop(3458, 5092); // Razormane Seer

			#endregion

			#region Razormane Backstabber

			AddMobDrop(3456, 5093); // Razormane Pathfinder
			AddMobDrop(3457, 5093); // Razormane Stalker

			#endregion

			#region Razormanager War Shield

			AddMobDrop(3459, 5094); // Razormane Warfrenzy

			#endregion
		}

		/// <summary>
		/// Returns a copy of the internal dictionary used as the database. The key is the mob ID, the value is a HashSet of the drops.
		/// </summary>
		public static Dictionary<uint, HashSet<uint>> Mobs
		{
			get
			{
				var ret = new Dictionary<uint, HashSet<uint>>();
				foreach (KeyValuePair<uint, HashSet<uint>> kvp in _mobs)
				{
					var copy = new HashSet<uint>(kvp.Value);
					ret.Add(kvp.Key, copy);
				}

				return ret;
			}
		}

		/// <summary>
		/// Returns a copy of the internal dictionary used as the database. The key is the game object ID, the value is a HashSet of the drops.
		/// </summary>
		public static Dictionary<uint, HashSet<uint>> GameObjects
		{
			get
			{
				var ret = new Dictionary<uint, HashSet<uint>>();
				foreach (KeyValuePair<uint, HashSet<uint>> kvp in _gameObjects)
				{
					var copy = new HashSet<uint>(kvp.Value);
					ret.Add(kvp.Key, copy);
				}

				return ret;
			}
		}

        /// <summary>
        /// Returns a copy of the internal dictionary used as the database. The key is the vendor ID, the value is a HashSet of the items for sale.
        /// </summary>
	    public static Dictionary<uint, HashSet<uint>> Vendors
	    {
	        get
	        {
	            var ret = new Dictionary<uint, HashSet<uint>>();
                foreach(KeyValuePair<uint, HashSet<uint>> kvp in _vendors)
                {
                    var copy = new HashSet<uint>(kvp.Value);
                    ret.Add(kvp.Key, copy);
                }

	            return ret;
	        }
	    }

		/// <summary>
		/// Adds a drop from a mob to the database.
		/// </summary>
		/// <param name="mobId">The ID of the mob.</param>
		/// <param name="dropItemId">The ID of the item the mob drops.</param>
		public static void AddMobDrop(uint mobId, uint dropItemId)
		{
			if (_mobs.ContainsKey(mobId))
			{
				_mobs[mobId].Add(dropItemId);
				return;
			}
			_mobs[mobId] = new HashSet<uint> { dropItemId };
		}

		/// <summary>
		/// Adds a drop from a game object to the database.
		/// </summary>
		/// <param name="gameObjId">The ID of the game object.</param>
		/// <param name="dropItemId">The ID of the item the game object drops.</param>
		public static void AddGameObjectDrop(uint gameObjId, uint dropItemId)
		{
			if (_gameObjects.ContainsKey(gameObjId))
			{
				_gameObjects[gameObjId].Add(dropItemId);
				return;
			}
			_gameObjects[gameObjId] = new HashSet<uint> { dropItemId };
		}

        /// <summary>
        /// Adds a vendor drop from a vendor to the database
        /// </summary>
        /// <param name="vendorId"></param>
        /// <param name="itemId"></param>
        public static void AddVendorDrop(uint vendorId, uint itemId)
        {
            if (_vendors.ContainsKey(vendorId))
			{
                _vendors[vendorId].Add(itemId);
				return;
			}
			_vendors[vendorId] = new HashSet<uint> { itemId };
        }

		/// <summary>
		/// Determines whether a mob drops an item according to this database.
		/// </summary>
		/// <param name="mobId">The ID of the mob.</param>
		/// <param name="itemId">The ID of the item.</param>
		/// <returns>A boolean indicating whether the mob drops the item according to this database.</returns>
		public static bool UnitDropsItem(uint mobId, uint itemId)
		{
			HashSet<uint> mob;
			if (_mobs.TryGetValue(mobId, out mob))
				return mob.Contains(itemId);
			return false;
		}

		/// <summary>
		/// Determines whether a game object drops an item according to this database.
		/// </summary>
		/// <param name="gameObjId">The ID of the game object.</param>
		/// <param name="itemId">The ID of the item.</param>
		/// <returns>A boolean indicating whether the game object drops the item according to this database.</returns>
		public static bool GameObjectDropsItem(uint gameObjId, uint itemId)
		{
			HashSet<uint> gameObj;
			if (_gameObjects.TryGetValue(gameObjId, out gameObj))
				return gameObj.Contains(itemId);
			return false;
		}

        /// <summary>
        /// Determines whether a vendor got an item for sale according to this database
        /// </summary>
        /// <param name="vendorId">The id of the vendor</param>
        /// <param name="itemId">The ID of the item</param>
        /// <returns>A boolean indicating whether the vendor sells this item according to this database.</returns>
        public static bool VendorSellsItem(uint vendorId, uint itemId)
        {
			HashSet<uint> vendor;
			if (_vendors.TryGetValue(vendorId, out vendor))
				return vendor.Contains(itemId);
			return false;
        }
	}
}
