using System.Threading.Tasks;
using System.Windows.Media;
using Bots.Quest.Resources;
using CommonBehaviors.Actions;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.Coroutines;
using Styx.Helpers;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;

namespace Bots.Quest.QuestOrder
{
    public class ForcedMoveTo : ForcedBehavior
    {
        private bool _isDone;

        public ForcedMoveTo(WoWPoint location, uint questId)
            : this(location, null, 1.5f, questId, NavType.Run)
        {
        }

        public ForcedMoveTo(WoWPoint location, string locationName, float precision, uint questId, NavType navType)
        {
            Location = location;
            LocationName = locationName ?? string.Format("<{0}, {1}, {2}>", location.X.ToStringInvariant(), location.Y.ToStringInvariant(), location.Z.ToStringInvariant());
            Precision = precision;
            QuestId = questId;
            NavType = navType;
        }

        public WoWPoint Location { get; private set; }
        public string LocationName { get; private set; }
        public float Precision { get; private set; }
        public uint QuestId { get; private set; }
        public NavType NavType { get; private set; }

        public override bool IsDone
        {
            get
            {
                if (QuestId == 0)
                    return _isDone;

                PlayerQuest quest = StyxWoW.Me.QuestLog.GetQuestById(QuestId);
                if (!_isDone && quest != null)
                    return quest.IsCompleted;

                return true;
            }
        }

        protected override Composite CreateBehavior()
        {
            return new ActionRunCoroutine(context => MoveToLocation());
        }

        private async Task<bool> MoveToLocation()
        {
            if (NavType != NavType.Run && Flightor.CanFly)
            {
                if (Location.DistanceSqr(StyxWoW.Me.Location) < 100f)
                {
                    await new Mount.ActionLandAndDismount().ExecuteCoroutine();
                    _isDone = true;
                    return true;
                }

                Flightor.MoveTo(Location, true);
                return true;
            }

            switch (Navigator.MoveTo(Location))
            {
                case MoveResult.ReachedDestination:
                    _isDone = true;
                    return true;
                case MoveResult.Failed:
                case MoveResult.PathGenerationFailed:
                    Logging.Write(Colors.OrangeRed, "MoveTo failed to move to the location: {0}", Location);
                    _isDone = true;
                    return false;
                default:
                    return true;
            }
        }

        public override void OnStart()
        {
            TreeRoot.GoalText = string.Format(QuestBotResources.ForcedMoveTo_OnStart_Moving_to__0_, LocationName);
        }
    }
}
