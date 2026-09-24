using System.Collections.Generic;
using Styx;
using Styx.CommonBot;
using Styx.CommonBot.AreaManagement;
using Styx.CommonBot.Profiles;
using Styx.CommonBot.Profiles.Quest.Order;


using Styx.WoWInternals;

namespace Bots.Quest
{
	public class QuestState
	{
		public static readonly QuestState Instance = new QuestState();

		public QuestState()
		{
			Order = new QuestOrder.QuestOrder();

			BotEvents.Profile.OnNewProfileLoaded += Profile_OnNewProfileLoaded;
		}

		private void Profile_OnNewProfileLoaded(BotEvents.Profile.NewProfileLoadedEventArgs args)
		{
			if (args.OldProfile == null || !ReferenceEquals(args.OldProfile.QuestOrder, args.NewProfile.QuestOrder))
				Reload(args.NewProfile);
		}

		internal void Reload(Profile profile)
		{
			if (profile == null)
			{
				Order.Nodes = new OrderNodeCollection();
				return;
			}

			if (Order.CurrentBehavior != null)
			{
				Order.CurrentBehavior.OnFinished();
				Order.CurrentBehavior.Dispose();
				Order.CurrentBehavior = null;
			}

			Order.Nodes = new OrderNodeCollection(profile.QuestOrder.Count);
			Order.Nodes.AddRange(profile.QuestOrder);
			Order.Nodes.IgnoreCheckpoints = profile.QuestOrder.IgnoreCheckpoints;

			if (Order.Nodes.Count <= 0)
				return;

			ObjectManager.Update();
			Order.UpdateNodes();
		}

		public QuestOrder.QuestOrder Order { get; private set; }
		public List<Vendor> CurrentVendors { get; set; }
		public List<Mailbox> CurrentMailboxes { get; set; }
		public GrindArea CurrentGrindArea { get; set; }
	}
}
