using System;
using System.Reflection;
using System.Windows.Forms;
using Bots.Grind;
using Bots.Quest.Actions;
using Bots.Quest.QuestOrder;
using Bots.Quest.Resources;
using CommonBehaviors.Actions;
using CommonBehaviors.Decorators;
using Levelbot;
using Levelbot.Actions.Combat;
using Levelbot.Decorators.Combat;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.POI;
using Styx.CommonBot.Profiles;
using Styx.CommonBot.Profiles.Quest.Order;
using Styx.Helpers;
using Styx.Localization;
using Styx.Pathing;
using Styx.TreeSharp;
using MathEx = Tripper.Tools.Math.MathEx;
using Sequence = Styx.TreeSharp.Sequence;

namespace Bots.Quest
{
    public class QuestBot : BotBase
    {
        private static Composite _root;

        public override string Name { get { return Globalization.QuestBot_Name; } }

        public override Composite Root { get { return _root ?? (_root = CreateRoot()); } }

        public override Form ConfigurationForm { get { return new FormLevelbotSettings(); } }

        public override PulseFlags PulseFlags { get { return PulseFlags.All; } }

        public override bool RequiresProfile { get { return true; } }

        public override bool RequiresProfileScope { get { return true; } }

        public static Composite CreateRoot()
        {
            return new PrioritySelector(
                new HookExecutor(
                    "Questbot_Main",
                    "The main quest root behavior location. This is the root of the quest bot.",
                    new PrioritySelector(
                        LevelBot.CreateDeathBehavior(),
                        LevelBot.CreateCombatBehavior(),
                        LevelBot.CreateLootBehavior(),
                        CreateNoBodyPullsBehavior(),
                        LevelBot.CreateVendorBehavior(),
                        CreateQuestBehavior(),
                        new ActionRunCoroutine(context => LevelBot.Roam()))));
        }

        public override void AddMonitorMembers()
        {
            dynamic statusObject = StatusObject;
            statusObject.ExperienceLeft = StyxWoW.Me.NextLevelExperience - StyxWoW.Me.Experience;
        }

        public override void Start()
        {
            if (ProfileManager.CurrentOuterProfile == null)
                throw new HonorbuddyUnableToStartException(QuestBotResources.QuestBot_Start_You_haven_t_loaded_a_profile_);

            if (ProfileManager.CurrentProfile == null)
                throw new HonorbuddyUnableToStartException(Globalization.NoViableSubProfile);

            if (ProfileManager.CurrentProfile.QuestOrder == null || ProfileManager.CurrentProfile.QuestOrder.Count <= 0)
                throw new HonorbuddyUnableToStartException(QuestBotResources.QuestBot_Start_Can_not_start_quest_bot___this_profile_does_not_contain_a_quest_order_);

            QuestState.Instance.Reload(ProfileManager.CurrentProfile);
            LootTargeting.Instance.IncludeTargetsFilter += LevelBot.LevelbotIncludeLootsFilter;
            Targeting.Instance.IncludeTargetsFilter += LevelBot.LevelBotIncludeTargetsFilter;
            QuestState.Instance.Order.OnNoMoreNodes += Order_OnNoMoreNodes;

            if (GlobalSettings.Instance.ProfileDebuggingMode && !CheckQuestBehaviors(ProfileManager.CurrentOuterProfile))
                throw new HonorbuddyUnableToStartException(QuestBotResources.QuestBot_Start_Could_not_construct_all_quest_behaviors_);

            CharacterSettings.Instance.RessAtSpiritHealers = false;
        }

        private static bool CheckQuestBehaviors(Profile profile)
        {
            bool success = true;
            if (profile.QuestOrder != null && profile.QuestOrder.Count > 0 && !CheckQuestBehaviors(profile.QuestOrder))
                success = false;

            foreach (Profile subProfile in profile.SubProfiles)
            {
                if (subProfile != null && subProfile.QuestOrder != null && subProfile.QuestOrder.Count > 0 && !CheckQuestBehaviors(subProfile.QuestOrder))
                    success = false;
            }

            return success;
        }

        private static bool CheckQuestBehaviors(INodeContainer nodeContainer)
        {
            bool success = true;
            foreach (OrderNode node in nodeContainer.GetNodes())
            {
                var childContainer = node as INodeContainer;
                if (childContainer != null && !CheckQuestBehaviors(childContainer))
                    success = false;

                var codeNode = node as CodeNode;
                if (codeNode == null)
                    continue;

                try
                {
                    var forcedBehavior = new ForcedCodeBehavior(codeNode);
                    forcedBehavior.OnFinished();
                    forcedBehavior.Dispose();
                }
                catch (TargetInvocationException ex)
                {
                    Logging.Write(QuestBotResources.QuestBot_CheckQuestBehaviors_Exception_thrown_while_trying_to_construct_quest_behavior__Tag__0__1__0__2_, Environment.NewLine, codeNode.Element.ToString(), ex.InnerException.ToString());
                    Logging.Write("----------------------");
                }
                catch (Exception ex)
                {
                    Logging.Write(QuestBotResources.QuestBot_CheckQuestBehaviors_Exception_thrown_while_trying_to_construct_quest_behavior__Tag__0__1__0__2_, Environment.NewLine, codeNode.Element.ToString(), ex.ToString());
                    Logging.Write("----------------------");
                }
            }

            return success;
        }

        private static void Order_OnNoMoreNodes(object sender, EventArgs e)
        {
            TreeRoot.Stop(QuestBotResources.PerformQuestObjectiveOrdered_Execute_Nothing_more_to_do__Stopping_bot_);
        }

        public override void Stop()
        {
            if (QuestState.Instance.Order.CurrentBehavior != null)
            {
                QuestState.Instance.Order.CurrentBehavior.Branch.Stop(null);
                QuestState.Instance.Order.CurrentBehavior.OnFinished();
                QuestState.Instance.Order.CurrentBehavior.Dispose();
                QuestState.Instance.Order.CurrentBehavior = null;
            }

            LootTargeting.Instance.IncludeTargetsFilter -= LevelBot.LevelbotIncludeLootsFilter;
            Targeting.Instance.IncludeTargetsFilter -= LevelBot.LevelBotIncludeTargetsFilter;
        }

        private float CalculatePathPrecision()
        {
            return MathEx.Clamp(StyxWoW.Me.MovementInfo.CurrentSpeed * 0.15f, 1.5f, 10f);
        }

        public override void Pulse()
        {
            Navigator.PathPrecision = CalculatePathPrecision();
        }

        private static Composite CreateNoBodyPullsBehavior()
        {
            return new Decorator(
                context => StyxWoW.Me.IsMoving && !StyxWoW.Me.Combat,
                new DecoratorIsNotPoiType(
                    PoiType.Kill,
                    new DecoratorNeedToFindTarget(
                        new Sequence(
                            new ActionSetTarget(),
                            new Wait(5, context => StyxWoW.Me.CurrentTarget != null,
                                new ActionSetPoi(context => new BotPoi(StyxWoW.Me.CurrentTarget, PoiType.Kill)))))));
        }

        private static PrioritySelector CreateQuestBehavior()
        {
            return new PrioritySelector(
                new HookExecutor(
                    "Questbot_Profile",
                    "The quest profile behavior. This is the location the order nodes in a questing profile are executed from.",
                    new ForcedBehaviorExecutor(QuestState.Instance.Order)));
        }
    }
}
