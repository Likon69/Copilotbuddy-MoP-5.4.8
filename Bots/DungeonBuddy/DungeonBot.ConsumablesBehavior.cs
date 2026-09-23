using System;
using System.Collections.Generic;
using System.Linq;
using CommonBehaviors.Actions;
using Styx;
using Styx.CommonBot;
using Styx.CommonBot.Coroutines;
using Styx.WoWInternals;
using Styx.TreeSharp;
using Styx.WoWInternals.WoWObjects;
using Styx.Pathing;
using Styx.Helpers;
using Bots.DungeonBuddy.Helpers;
using Action = Styx.TreeSharp.Action;

namespace Bots.DungeonBuddy
{
    public partial class DungeonBot
    {
        private const uint BanquetoftheWokId = 214242; // agi
        private const uint BanquetoftheOvenId = 214248;
        private const uint BanquetoftheGrillId = 214240;
        private const uint GreatPandarenBanquetId = 209805;
        private const uint BanquetoftheSteamerId = 214246;
        private const uint BanquetoftheBrewId = 214250;
        private const uint BanquetofthePotId = 214244; // str

        private readonly uint[] _buffTableIds = new[]
                                                {
                                                    BanquetoftheWokId, BanquetoftheOvenId, BanquetoftheGrillId, GreatPandarenBanquetId, BanquetoftheSteamerId, BanquetoftheBrewId,
                                                    BanquetofthePotId
                                                };

        private readonly uint[] _refreshmentTableIds = new uint[] { 186812, 207386, 207387, 211363 };

        private WoWGameObject BuffFoodTable
        {
            get
            {
                return
                    ObjectManager.GetObjectsOfType<WoWGameObject>()
                                 .Where(i => _buffTableIds.Contains(i.Entry) && Me.GroupInfo.RaidMemberGuids.Any(guid => guid == i.CreatedByGuid) && i.Distance < 20)
                                 .OrderBy(g => g.DistanceSqr)
                                 .FirstOrDefault();
            }
        }

        private WoWGameObject MageTable
        {
            get
            {
                return
                    ObjectManager.GetObjectsOfType<WoWGameObject>()
                                 .Where(i => _refreshmentTableIds.Contains(i.Entry) && Me.GroupInfo.RaidMemberGuids.Any(guid => guid == i.CreatedByGuid) && i.Distance < 20)
                                 .OrderBy(g => g.DistanceSqr)
                                 .FirstOrDefault();
            }
        }

        private WoWGameObject SoulWell
        {
            get
            {
                return
                    ObjectManager.GetObjectsOfType<WoWGameObject>()
                                 .Where(i => i.Entry == 181621 && Me.GroupInfo.RaidMemberGuids.Any(guid => guid == i.CreatedByGuid) && i.Distance < 20)
                                 .OrderBy(g => g.DistanceSqr)
                                 .FirstOrDefault();
            }
        }

        private int CarriedMageFoodCount
        {
            get
            {
                return
                    (int)
                    Me.CarriedItems.Sum(
                        i =>
                        i != null && i.ItemInfo != null && i.ItemInfo.ItemClass == WoWItemClass.Consumable && i.HasSpell("Refreshment")
                            ? i.StackCount
                            : 0);
            }
        }

        private bool HasHealthStone
        {
            get { return StyxWoW.Me.CarriedItems.Any(i => i != null && i.HasSpell("Healthstone")); }
        }

        private Composite CreateSummonedComsumableBehavior()
        {
            return new Decorator(ctx => !Me.Combat, new PrioritySelector(CreateCollectConsumablesBehavior()));
        }

        private Composite CreateCollectConsumablesBehavior()
        {
            return new PrioritySelector(
                ctx =>
                {
                    WoWGameObject consumableObject = null;

                    if (CarriedMageFoodCount < 80 && StyxWoW.Me.FreeNormalBagSlots > 1)
                        consumableObject = MageTable;

                    if (consumableObject == null && !HasHealthStone && StyxWoW.Me.FreeNormalBagSlots > 1)
                        consumableObject = SoulWell;

                    if (consumableObject == null && !Me.HasAura("Well Fed") && !Me.Combat)
                        consumableObject = BuffFoodTable;

                    return consumableObject;
                },
                new Decorator(
                    ctx => ctx != null,
                    new PrioritySelector(
                        new Decorator(
                            obj => !((WoWGameObject)obj).CanUseNow(),
                            new Action(
                                obj =>
                                Navigator.GetRunStatusFromMoveResult(
                                    Navigator.MoveTo(WoWMathHelper.CalculatePointFrom(StyxWoW.Me.Location, ((WoWGameObject)obj).Location, 4))))),
                        new Decorator(
                            obj => ((WoWGameObject)obj).CanUseNow(),
                            new Sequence(
                                new Action(obj => Logger.Write("Collecting consumable from {0}", ((WoWGameObject)obj).Name)),
                                new DecoratorContinue(ctx => Me.Mounted, new ActionRunCoroutine(ctx => CommonCoroutines.Dismount("Collectiong consumable"))),
                                new DecoratorContinue(ctx => Me.IsMoving, new Action(ctx => WoWMovement.MoveStop())),
                                new WaitContinue(2, ctx => !Me.IsMoving, new ActionAlwaysSucceed()),
                                new Action(obj => ((WoWGameObject)obj).Interact()),
                                new WaitContinue(2, ctx => false, new ActionAlwaysSucceed()),
                                new WaitContinue<WoWGameObject>(
                                    12, obj => !_buffTableIds.Contains(obj.Entry) || Me.HasAura("Well Fed") || Me.Combat, new ActionAlwaysSucceed()))))));
        }
    }
}
