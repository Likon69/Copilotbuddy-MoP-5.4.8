using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace PartyBot.Resources
{
    [GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [CompilerGenerated]
    [DebuggerNonUserCode]
    public class PartyBotResources
    {
        private static ResourceManager _resourceManager;
        private static CultureInfo _culture;

        internal PartyBotResources()
        {
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static ResourceManager ResourceManager
        {
            get
            {
                if (ReferenceEquals(_resourceManager, null))
                {
                    _resourceManager = new ResourceManager(
                        "PartyBot.Resources.PartyBotResources",
                        typeof(PartyBotResources).Assembly);
                }

                return _resourceManager;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static CultureInfo Culture
        {
            get { return _culture; }
            set { _culture = value; }
        }

        public static string BotName
        {
            get { return ResourceManager.GetString("BotName", _culture); }
        }

        public static string DiscoBot_CreateCombatBehavior_Moving_to_target
        {
            get { return ResourceManager.GetString("DiscoBot_CreateCombatBehavior_Moving_to_target", _culture); }
        }

        public static string DiscoBot_CreateCombatBehavior_Pulling
        {
            get { return ResourceManager.GetString("DiscoBot_CreateCombatBehavior_Pulling", _culture); }
        }

        public static string DiscoBot_CreateCombatBehavior_Setting_target_to_pull
        {
            get { return ResourceManager.GetString("DiscoBot_CreateCombatBehavior_Setting_target_to_pull", _culture); }
        }

        public static string DiscoBot_CreateDeathBehavior__PartyBot__Waited_3_minutes_and_we_got_no_ress__Releasing_from_corpse_
        {
            get { return ResourceManager.GetString("DiscoBot_CreateDeathBehavior__PartyBot__Waited_3_minutes_and_we_got_no_ress__Releasing_from_corpse_", _culture); }
        }

        public static string DiscoBot_CreateDeathBehavior__PartyBot__Waiting_for_ress_
        {
            get { return ResourceManager.GetString("DiscoBot_CreateDeathBehavior__PartyBot__Waiting_for_ress_", _culture); }
        }

        public static string DiscoBot_CreateEventBehavior__PartyBot__Accepting_dungeon_invite
        {
            get { return ResourceManager.GetString("DiscoBot_CreateEventBehavior__PartyBot__Accepting_dungeon_invite", _culture); }
        }

        public static string DiscoBot_CreateEventBehavior__PartyBot__Accepting_group_invite
        {
            get { return ResourceManager.GetString("DiscoBot_CreateEventBehavior__PartyBot__Accepting_group_invite", _culture); }
        }

        public static string DiscoBot_CreateEventBehavior__PartyBot__Accepting_shared_quest
        {
            get { return ResourceManager.GetString("DiscoBot_CreateEventBehavior__PartyBot__Accepting_shared_quest", _culture); }
        }

        public static string DiscoBot_CreateEventBehavior__PartyBot__Role_Check_is_in_progress
        {
            get { return ResourceManager.GetString("DiscoBot_CreateEventBehavior__PartyBot__Role_Check_is_in_progress", _culture); }
        }

        public static string DiscoBot_CreatePartyBotBehavior__PartyBot__Following_Leader
        {
            get { return ResourceManager.GetString("DiscoBot_CreatePartyBotBehavior__PartyBot__Following_Leader", _culture); }
        }

        public static string DiscoBot_CreatePartyBotBehavior__PartyBot__Killing_something
        {
            get { return ResourceManager.GetString("DiscoBot_CreatePartyBotBehavior__PartyBot__Killing_something", _culture); }
        }

        public static string DiscoBot_CreatePartyBotBehavior__PartyBot__LeaderOnDifferentMap
        {
            get { return ResourceManager.GetString("DiscoBot_CreatePartyBotBehavior__PartyBot__LeaderOnDifferentMap", _culture); }
        }

        public static string DiscoBot_CreatePartyBotBehavior__PartyBot__Vendoring
        {
            get { return ResourceManager.GetString("DiscoBot_CreatePartyBotBehavior__PartyBot__Vendoring", _culture); }
        }

        public static string DiscoBot_NoLeaderNameProvided
        {
            get { return ResourceManager.GetString("DiscoBot_NoLeaderNameProvided", _culture); }
        }

        public static string DiscoBot_WoWChat_Party__PartyBot__Dancing
        {
            get { return ResourceManager.GetString("DiscoBot_WoWChat_Party__PartyBot__Dancing", _culture); }
        }

        public static string DiscoBot_WoWChat_Party__PartyBot__Entering_Dungeon
        {
            get { return ResourceManager.GetString("DiscoBot_WoWChat_Party__PartyBot__Entering_Dungeon", _culture); }
        }

        public static string DiscoBot_WoWChat_Party__PartyBot__Leaving_Battleground
        {
            get { return ResourceManager.GetString("DiscoBot_WoWChat_Party__PartyBot__Leaving_Battleground", _culture); }
        }

        public static string DiscoBot_WoWChat_Party__PartyBot__Leaving_Dungeon
        {
            get { return ResourceManager.GetString("DiscoBot_WoWChat_Party__PartyBot__Leaving_Dungeon", _culture); }
        }

        public static string DiscoBot_WoWChat_Party__PartyBot__Someone_told_me_to_go_sell_
        {
            get { return ResourceManager.GetString("DiscoBot_WoWChat_Party__PartyBot__Someone_told_me_to_go_sell_", _culture); }
        }

        public static string DiscoBot_WoWChat_Party__PartyBot__Someone_told_me_to_mail_
        {
            get { return ResourceManager.GetString("DiscoBot_WoWChat_Party__PartyBot__Someone_told_me_to_mail_", _culture); }
        }

        public static string DiscoBot_WoWChat_Party__PartyBot__Someone_told_me_to_repair_
        {
            get { return ResourceManager.GetString("DiscoBot_WoWChat_Party__PartyBot__Someone_told_me_to_repair_", _culture); }
        }

        public static string DiscoBot_WoWChat_Party__PartyBot__Someone_told_me_to_train_
        {
            get { return ResourceManager.GetString("DiscoBot_WoWChat_Party__PartyBot__Someone_told_me_to_train_", _culture); }
        }
    }
}
