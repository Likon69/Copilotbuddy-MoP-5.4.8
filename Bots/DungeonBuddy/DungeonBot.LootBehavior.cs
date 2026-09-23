using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Bots.DungeonBuddy.Helpers;
using Buddy.Coroutines;
using CommonBehaviors.Actions;
using Styx;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.CommonBot.Coroutines;
using Styx.CommonBot.Frames;
using Styx.CommonBot.POI;
using Styx.Helpers;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace Bots.DungeonBuddy
{
    public partial class DungeonBot
    {
        private static ulong _lootTargetGuid;
        private static Stopwatch _lootTimer;
        private static int _lootAttempts;
        private static int _interactAttempts;
        private readonly WaitTimer _lootInteractTimer = new WaitTimer(TimeSpan.FromMilliseconds(1000));

        private bool AutoLoot
        {
            get { return Lua.GetReturnVal<bool>("if GetCVar('autoLootDefault') == 0 then return nil end return 1", 0u); }
        }

        private Composite CreateLootBehavior()
        {
            return new ActionRunCoroutine(context => Loot());
        }

        private async Task<bool> Loot()
        {
            if ((Me.IsTank() || Me.IsHealer()) && !Targeting.Instance.IsEmpty() && Targeting.Instance.FirstUnit.IsTargetingMyRaidMember)
            {
                return false;
            }

            if (!BotPoi.Current.Type.IsType(PoiType.Kill, PoiType.Harvest, PoiType.Loot, PoiType.Skin))
            {
                BotPoi lootPoi = GetLootPoi();
                if (lootPoi != null)
                {
                    BotPoi.Current = lootPoi;
                }
            }

            if (BotPoi.Current.Type.IsType(PoiType.Harvest, PoiType.Loot, PoiType.Skin))
            {
                return await LootObject();
            }

            return false;
        }

        private async Task<bool> LootObject()
        {
            WoWObject lootObject = BotPoi.Current.AsObject;
            if (lootObject == null)
            {
                if (!Navigator.AtLocation(BotPoi.Current.Location))
                {
                    MoveResult moveResult = await CommonCoroutines.MoveTo(BotPoi.Current.Location);
                    if (!moveResult.IsSuccessful())
                    {
                        BlacklistLoot(_lootTargetGuid, "Failed to path to loot");
                        return false;
                    }
                    return true;
                }

                BotPoi.Clear("Loot not found at location");
                return false;
            }

            if (lootObject.Guid != _lootTargetGuid)
            {
                _lootTargetGuid = lootObject.Guid;
                _lootTimer = Stopwatch.StartNew();
                _lootAttempts = 0;
                _interactAttempts = 0;
            }

            if (Me.IsBeingAttacked)
            {
                BotPoi.Clear("Attacked while looting");
                _lootTargetGuid = 0uL;
                return false;
            }

            if (_lootTimer.ElapsedMilliseconds > 25000L)
            {
                BlacklistLoot(_lootTargetGuid, "It took too long to loot {0}", lootObject.SafeName);
                return false;
            }

            WoWGameObject gameObject = lootObject as WoWGameObject;
            WoWUnit unit = lootObject as WoWUnit;
            if ((unit != null && !unit.Lootable && !unit.CanSkin) ||
                (gameObject != null && !gameObject.CanLoot && !LootFrame.Instance.IsVisible))
            {
                BlacklistLoot(_lootTargetGuid, "{0} is not lootable", lootObject.SafeName);
                return false;
            }

            if (_interactAttempts >= 4)
            {
                BlacklistLoot(_lootTargetGuid, "Failed to open loot frame on {0} after {1} attempts ", lootObject.SafeName, _interactAttempts);
                return false;
            }

            if (_lootAttempts >= 10)
            {
                BlacklistLoot(_lootTargetGuid, "Failed to loot {0} after {1} attempts ", lootObject.SafeName, 10);
                return false;
            }

            if (Me.IsCasting)
            {
                return true;
            }

            if (!lootObject.WithinInteractRange)
            {
                MoveResult moveResult = await CommonCoroutines.MoveTo(lootObject.Location, lootObject.SafeName);
                if (!moveResult.IsSuccessful())
                {
                    BlacklistLoot(_lootTargetGuid, "Failed to path to object {0}", lootObject.SafeName);
                    return false;
                }
                return true;
            }

            if (!LootFrame.Instance.IsVisible)
            {
                if (Me.IsMoving)
                {
                    await ScriptHelpers.StopMovingIfMoving("looting");
                }

                if (_lootInteractTimer.IsFinished)
                {
                    lootObject.Interact();
                    await CommonCoroutines.SleepForLagDuration();
                    _interactAttempts++;
                    _lootInteractTimer.Reset();
                }
                return true;
            }

            if (_lootInteractTimer.IsFinished)
            {
                if (_lootAttempts > 0 || !AutoLoot)
                {
                    Lua.DoString("for i=1, GetNumLootItems() do LootSlot(i) ConfirmLootSlot(i) end");
                }
                Navigator.NavigationProvider.StuckHandler.Reset();
                _lootAttempts++;
                _lootInteractTimer.Reset();
            }

            return true;
        }

        private void BlacklistLoot(ulong guid, string reasonFormat, params object[] reasonArguments)
        {
            string reason = string.Format(reasonFormat, reasonArguments);
            BlacklistFlags flags = BotPoi.Current.Type == PoiType.Loot ? BlacklistFlags.Loot : BlacklistFlags.Node;
            Blacklist.Add(guid, flags, TimeSpan.FromMinutes(25.0), reason);
            if (LootFrame.Instance.IsVisible)
            {
                LootFrame.Instance.Close();
            }
            BotPoi.Clear("Loot blacklisted");
        }

        private BotPoi GetLootPoi()
        {
            if (Me.FreeNormalBagSlots <= DungeonBuddySettings.Instance.InsideInstanceMinFreeBagSlots || LootTargeting.Instance.IsEmpty())
            {
                return null;
            }

            WoWObject firstObject = LootTargeting.Instance.FirstObject;
            PoiType? poiType = GetLootPoiType(firstObject);
            if (!poiType.HasValue)
            {
                return null;
            }

            float? pathDistance = DungeonNavigator.PathDistance(Me.Location, firstObject.Location);
            if (!pathDistance.HasValue)
            {
                BlacklistFlags flags = poiType == PoiType.Loot ? BlacklistFlags.Loot : BlacklistFlags.Node;
                if (!Blacklist.Contains(firstObject, flags))
                {
                    Blacklist.Add(firstObject, flags, TimeSpan.FromMinutes(3.0), "Cannot navigate to loot");
                }
                return null;
            }

            if (pathDistance > LootTargeting.LootRadius)
            {
                return null;
            }

            return new BotPoi(firstObject, poiType.Value);
        }

        private PoiType? GetLootPoiType(WoWObject lootObject)
        {
            WoWUnit unit = lootObject as WoWUnit;
            if (unit != null)
            {
                if (LootTargeting.SkinMobs && unit.SkinType == WoWCreatureSkinType.Leather && unit.Skinnable && unit.CanSkin && !Blacklist.Contains(lootObject, BlacklistFlags.Node))
                {
                    return PoiType.Skin;
                }
                if (unit.Lootable && unit.CanLoot && !Blacklist.Contains(lootObject, BlacklistFlags.Loot))
                {
                    return PoiType.Loot;
                }
            }

            WoWGameObject gameObject = lootObject as WoWGameObject;
            if (gameObject != null && gameObject.CanLoot && !Blacklist.Contains(lootObject, BlacklistFlags.Node))
            {
                return PoiType.Harvest;
            }

            return null;
        }
    }
}
