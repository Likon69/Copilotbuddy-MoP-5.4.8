using System;
using System.Collections.Generic;
using System.Linq;
using Styx.CommonBot.AreaManagement;
using Styx.CommonBot.Profiles;
using Styx.Helpers;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace Styx.CommonBot
{
    public class LootTargeting : Targeting
    {
        private static LootTargeting _instance;

        public static bool LootFrameIsOpen
        {
            get { return StyxWoW.Memory.Read<uint>(StyxWoW.Offsets.Statics.LootFrame) != 0; }
        }

        public Dictionary<ulong, DateTime> AlreadySkinned = new Dictionary<ulong, DateTime>();
        public Dictionary<ulong, DateTime> AlreadyLooted = new Dictionary<ulong, DateTime>();
        public Dictionary<ulong, DateTime> AlreadyHarvested = new Dictionary<ulong, DateTime>();

        #region Filters

        protected override void DefaultRemoveTargetsFilter(List<WoWObject> objects)
        {
            for (int i = objects.Count - 1; i >= 0; i--)
            {
                WoWObject obj = objects[i];
                WoWUnit unit = obj as WoWUnit;

                if (unit != null)
                {
                    if (unit.IsDisabled)
                    {
                        objects.RemoveAt(i);
                        continue;
                    }

                    if ((Blacklist.Contains(unit, BlacklistFlags.Loot) || !unit.CanLoot) && (!LootPredictor.WasKilledByMe(unit) || !LootPredictor.CanLoot(unit)))
                    {
                        if ((!unit.CanSkin && !unit.Skinnable) || !SkinMobs || (!NinjaSkin && !unit.KilledByMe) || Blacklist.Contains(unit, BlacklistFlags.Node))
                        {
                            objects.RemoveAt(i);
                        }
                    }
                }
                else
                {
                    WoWGameObject gameObject = obj as WoWGameObject;
                    if (gameObject == null || !gameObject.IsValid || Blacklist.Contains(gameObject, BlacklistFlags.Node))
                    {
                        objects.RemoveAt(i);
                    }
                }
            }
        }

        protected override void DefaultIncludeTargetsFilter(List<WoWObject> incomingObjects, HashSet<WoWObject> outgoingObjects)
        {
        }

        protected override void DefaultTargetWeight(List<TargetPriority> objs)
        {
            for (int i = 0; i < objs.Count; i++)
            {
                objs[i].Score += 400f - (float)objs[i].Object.Distance * 2f;
            }
        }

        #endregion

        private LootTargeting()
        {
            DisplayTargetingExceptions = true;
            MaxTargets = 3;
        }

        public new static LootTargeting Instance
        {
            get { return _instance ?? (_instance = new LootTargeting()); }
        }

        public WoWObject FirstObject
        {
            get
            {
                return LootingList.Count != 0 ? LootingList[0] : null;
            }
        }

        public List<WoWObject> LootingList
        {
            get { return ObjectList; }
        }

        #region Options

        public static bool SkinMobs
        {
            get { return CharacterSettings.Instance.SkinMobs; }
        }

        public static bool LootMobs
        {
            get
            {
                GrindArea grindArea = StyxWoW.AreaManager != null ? StyxWoW.AreaManager.CurrentGrindArea : null;
                if (grindArea != null && grindArea.LootMobs != null)
                {
                    return grindArea.LootMobs.Value;
                }

                Profile currentProfile = ProfileManager.CurrentProfile;
                if (currentProfile != null && currentProfile.LootMobs != null)
                {
                    return currentProfile.LootMobs.Value;
                }

                return CharacterSettings.Instance.LootMobs;
            }
        }

        public static bool NinjaSkin
        {
            get { return CharacterSettings.Instance.NinjaSkin; }
        }

        public static bool HarvestMinerals
        {
            get { return CharacterSettings.Instance.HarvestMinerals; }
        }

        public static bool HarvestHerbs
        {
            get { return CharacterSettings.Instance.HarvestHerbs; }
        }

        public static bool LootChests
        {
            get { return CharacterSettings.Instance.LootChests; }
        }

        public static double LootRadius
        {
            get
            {
                GrindArea currentGrindArea = StyxWoW.AreaManager.CurrentGrindArea;
                if (currentGrindArea != null && currentGrindArea.LootRadius != null && currentGrindArea.LootRadius.Value > 5.0)
                {
                    return currentGrindArea.LootRadius.Value;
                }

                Profile currentProfile = ProfileManager.CurrentProfile;
                if (currentProfile != null && currentProfile.LootRadius != null)
                {
                    return currentProfile.LootRadius.Value;
                }

                return CharacterSettings.Instance.LootRadius;
            }
        }

        #endregion

        protected override List<WoWObject> GetInitialObjectList()
        {
            return ObjectManager.GetObjectsOfType<WoWObject>(true, true).ToList();
        }
    }
}
