using System;
using Bots.Quest.Resources;
using CommonBehaviors.Actions;
using CommonBehaviors.Decorators;
using Styx;
using Styx.CommonBot;
using Styx.CommonBot.POI;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace Bots.Quest.QuestOrder
{
    public class ForcedUseItem : ForcedBehavior
    {
        private bool _isDone;
        private Composite _branch;

        public ForcedUseItem(Func<WoWItem> itemRetriever, Func<WoWObject> targetRetriever, bool forceUse, uint questId, WoWPoint location, NavType navType)
        {
            if (itemRetriever == null)
                throw new ArgumentNullException("itemRetriever");

            ItemRetriever = itemRetriever;
            if (targetRetriever == null)
                targetRetriever = () => null;

            TargetRetriever = targetRetriever;
            ForceUse = forceUse;
            QuestId = questId;
            Location = location;
            NavType = navType;
        }

        public Func<WoWItem> ItemRetriever { get; private set; }
        public Func<WoWObject> TargetRetriever { get; private set; }
        public bool ForceUse { get; private set; }
        public uint QuestId { get; private set; }
        public WoWPoint Location { get; private set; }
        public NavType NavType { get; private set; }

        public override bool IsDone
        {
            get
            {
                PlayerQuest quest = StyxWoW.Me.QuestLog.GetQuestById(QuestId);
                if (!_isDone)
                    return quest != null && quest.IsCompleted;

                return true;
            }
        }

        protected override Composite CreateBehavior()
        {
            return _branch ?? (_branch =
                new PrioritySelector(context => ItemRetriever(),
                    new Decorator(context => StyxWoW.Me.Location.DistanceSqr(Location) > Navigator.PathPrecision * Navigator.PathPrecision,
                        new PrioritySelector(
                            new DecoratorIsNotPoiType(PoiType.Hotspot,
                                new ActionSetPoi(context => new BotPoi(Location, PoiType.Hotspot, NavType))),
                            new ActionMoveToPoi())),
                    new DecoratorIsPoiType(PoiType.Hotspot, new ActionClearPoi("Reached at location")),
                    new Decorator(context => context != null,
                        new Sequence(
                            new DecoratorContinue(context => StyxWoW.Me.IsMoving,
                                new Styx.TreeSharp.Action(context => WoWMovement.MoveStop())),
                            new WaitContinue(3, context => !StyxWoW.Me.IsMoving,
                                new Sequence(
                                    new Mount.ActionLandAndDismount("UseItem"),
                                    new Styx.TreeSharp.Action(context =>
                                    {
                                        var item = (WoWItem)context;
                                        WoWObject target = TargetRetriever();
                                        item.Use(target != null ? target.Guid : 0uL, ForceUse);
                                    }),
                                    new SleepForLagDuration(),
                                    new WaitContinue(30, context => !StyxWoW.Me.IsCasting,
                                        new Styx.TreeSharp.Action(context => _isDone = true))))))));
        }

        public override void OnStart()
        {
            PlayerQuest quest = StyxWoW.Me.QuestLog.GetQuestById(QuestId);
            if (quest != null)
                TreeRoot.GoalText = string.Format(QuestBotResources.ForcedUseItem_OnStart_Using_item_for__0_, quest.Name);
            else
                _isDone = true;
        }

        public override string ToString()
        {
            return string.Format(QuestBotResources.ForcedUseItem_ToString__ForcedUseItem_ForceUse___0__, ForceUse);
        }
    }
}
