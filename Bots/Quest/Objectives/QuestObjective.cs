using System;
using System.Collections.Generic;
using System.Linq;
using Bots.Quest.Resources;
using Styx;
using Styx.CommonBot.AreaManagement;
using Styx.CommonBot.Profiles;
using Styx.CommonBot.Profiles.Quest;



using Styx.TreeSharp;
using Styx.WoWInternals;
using Tripper.Tools.Math;

namespace Bots.Quest.Objectives
{
	public abstract class QuestObjective : IDisposable, IEquatable<QuestObjective>
	{
		protected QuestObjective(PlayerQuest quest, List<WoWQuestStep> questSteps, List<QuestObjective> prerequisites, WoWPoint forcedHotspot, NavType navType)
		{
			if (quest == null)
				throw new ArgumentNullException("quest");

			if (quest.Id == 0)
				throw new ArgumentException(QuestBotResources.QuestObjective_QuestObjective_Quest_passed_has_an_invalid_quest_ID_, "quest");

			if (!StyxWoW.Me.QuestLog.ContainsQuest(quest.Id))
				throw new ArgumentException(QuestBotResources.QuestObjective_QuestObjective_Quest_passed_is_not_contained_in_the_quest_log_, "quest");

			Quest = quest;
			NavType = navType;

			QuestSteps = questSteps ?? new List<WoWQuestStep>();

			QuestArea = new QuestArea(quest, QuestSteps);
            StyxWoW.AreaManager.Add(QuestArea);

			Prerequisites = prerequisites ?? new List<QuestObjective>();

		    Profile curProfile = ProfileManager.CurrentProfile;

            QuestInfo info;
		    OverridedQuestInfo = curProfile != null && (info = curProfile.FindQuest(quest.Id)) != null ? info : null;
		    ForcedHotspot = forcedHotspot;
		    _guid = Guid.NewGuid();
		}

		private Guid _guid;

		public WoWPoint ForcedHotspot { get; private set; }

		public NavType NavType { get; private set; }

		public QuestArea QuestArea { get; private set; }

		public List<WoWQuestStep> QuestSteps { get; private set; }

		public PlayerQuest Quest { get; private set; }

		public QuestInfo OverridedQuestInfo { get; private set; }

		public abstract bool IsCompleted { get; }

		public abstract bool CanComplete { get; }

		public readonly List<QuestObjective> Prerequisites;
		public bool DonePrerequisites { get { return Prerequisites.All(qo => qo.IsCompleted); } }

		public abstract Composite CreateBranch();

		public abstract WoWPoint GetObjectiveLocation();

		public virtual void OnStart()
		{
		}

		protected List<WoWQuestStep> GetDistanceSortedQuestSteps()
		{
			var steps = new List<WoWQuestStep>();
			steps.AddRange(QuestSteps);

			WoWPoint myLoc = StyxWoW.Me.Location;
			steps.Sort((step1, step2) => QuestStepComparer(myLoc, step1, step2));
			return steps;
		}

		protected WoWQuestStep GetClosestQuestStep()
		{
			if (QuestSteps == null || QuestSteps.Count <= 0)
				return new WoWQuestStep();

			WoWPoint myLoc = StyxWoW.Me.Location;
			WoWQuestStep closest = QuestSteps[0];
			float closestDist = myLoc.Distance2DSqr(new WoWPoint(closest.StepPosition.X, closest.StepPosition.Y, 0f));
			for (int i = 1; i < QuestSteps.Count; i++)
			{
				WoWQuestStep step = QuestSteps[i];
				float dist = myLoc.Distance2DSqr(new WoWPoint(step.StepPosition.X, step.StepPosition.Y, 0f));
				if (dist < closestDist)
				{
					closest = step;
					closestDist = dist;
				}
			}

			return closest;
		}

		private static int QuestStepComparer(WoWPoint myLoc, WoWQuestStep step1, WoWQuestStep step2)
		{
			var pnt1 = new WoWPoint(step1.StepPosition.X, step1.StepPosition.Y, 0f);
			var pnt2 = new WoWPoint(step2.StepPosition.X, step2.StepPosition.Y, 0f);
			return myLoc.Distance2DSqr(pnt1).CompareTo(myLoc.Distance2DSqr(pnt2));
		}

		public bool IsPointInArea(Vector2 pnt)
		{
			var pnt3 = new Vector3(pnt, 0f);
			return IsPointInArea(pnt3);
		}

		public bool IsPointInArea(Vector3 pnt)
		{
			List<List<Vector3>> areas = QuestArea.AreaDefinitions;
			return areas.Any(t => IsPointInPoly(t, ref pnt));
		}

		private static bool IsPointInPoly(IList<Vector3> verts, ref Vector3 test)
		{
			bool c = false;
			for (int i = 0, j = verts.Count - 1; i < verts.Count - 1; j = i++)
				if (verts[i].Y > test.Y != verts[j].Y > test.Y && test.X < (verts[j].X - verts[i].X) * (test.Y - verts[i].Y) / (verts[j].Y - verts[i].Y) + verts[i].X)
					c = !c;

			return c;
		}

		public virtual void Dispose()
		{
		}

		public bool Equals(QuestObjective other)
		{
			if (ReferenceEquals(null, other))
				return false;
			return ReferenceEquals(this, other) || other._guid.Equals(_guid);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			return Equals(obj as QuestObjective);
		}

		public override int GetHashCode()
		{
			return _guid.GetHashCode();
		}

		public static bool operator ==(QuestObjective left, QuestObjective right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(QuestObjective left, QuestObjective right)
		{
			return !Equals(left, right);
		}
	}
}
