using Styx.WoWInternals;

namespace Styx.CommonBot
{
    /// <summary>An extended version of the WoWChat class. Exposes much more in terms of availability of chat messages.</summary>
    /// <remarks>Created 9/23/2010.</remarks>
    public static class Chat
    {
        #region Delegates

        /// <summary>A simple generic delegate to help simplify chat based events.</summary>
        /// <remarks>Created 9/23/2010.</remarks>
        /// <typeparam name="T">.</typeparam>
        /// <param name="e">The event arguments.</param>
        public delegate void ChatMessageHandlerEx<T>(T e) where T : ChatMessageBaseEventArgs;

        #endregion

        #region EventArgs

        #region Nested type: ChatAddonEventArgs

        /// <summary>Additional information for chat addon events. </summary>
        /// <remarks>Created 9/23/2010.</remarks>
        public class ChatAddonEventArgs : ChatMessageBaseEventArgs
        {
            internal ChatAddonEventArgs(LuaEventArgs from) : base(from)
            {
            }

            /// <summary>Gets the prefix.</summary>
            /// <value>The prefix.</value>
            public string Prefix { get { return Args[0].ToString(); } }

            /// <summary>Gets the message.</summary>
            /// <value>The message.</value>
            public string Message { get { return Args[1].ToString(); } }

            /// <summary>PARTY, RAID, GUILD, BATLEGROUND, or WHISPER</summary>
            /// <value>The type.</value>
            public string Type { get { return Args[2].ToString(); } }

            /// <summary>Gets the sender.</summary>
            /// <value>The sender.</value>
            public string Sender { get { return Args[3].ToString(); } }
        }

        #endregion

        #region Nested type: ChatAuthoredEventArgs

        /// <summary>Additional information for chat events with an author. </summary>
        /// <remarks>Created 9/23/2010.</remarks>
        public class ChatAuthoredEventArgs : ChatSimpleMessageEventArgs
        {
            internal ChatAuthoredEventArgs(LuaEventArgs from) : base(from)
            {
            }

            /// <summary>Gets the author.</summary>
            /// <value>The author.</value>
            public string Author { get { return Args[1].ToString(); } }
        }

        #endregion

        #region Nested type: ChatChannelSpecificEventArgs

        /// <summary>Additional information for chat channel specific events. </summary>
        /// <remarks>Created 9/23/2010.</remarks>
        public class ChatChannelSpecificEventArgs : ChatLanguageSpecificEventArgs
        {
            internal ChatChannelSpecificEventArgs(LuaEventArgs from) : base(from)
            {
            }

            /// <summary>Gets the channel name with number.</summary>
            /// <value>The channel name with number.</value>
            public string ChannelNameWithNumber { get { return Args[3].ToString(); } }

            /// <summary>Gets target for the.</summary>
            /// <value>The target.</value>
            public string Target { get { return Args[4].ToString(); } }

            /// <summary>Gets the chat flags.</summary>
            /// <value>The chat flags.</value>
            public string ChatFlags { get { return Args[5].ToString(); } }

            /// <summary>Gets the zone flags.</summary>
            /// <value>The zone flags.</value>
            public int ZoneFlags { get { return (int) (double) Args[6]; } }

            /// <summary>Gets the channel number.</summary>
            /// <value>The channel number.</value>
            public int ChannelNumber { get { return (int) Args[7]; } }

            /// <summary>Gets the name of the channel.</summary>
            /// <value>The name of the channel.</value>
            public string ChannelName { get { return Args[8].ToString(); } }

            /// <summary>Gets the identifier of the line.</summary>
            /// <value>The identifier of the line.</value>
            public int LineId { get { return (int) (double) Args[9]; } }
        }

        #endregion

        #region Nested type: ChatGuildEventArgs

        /// <summary>Additional information for chat guild events. </summary>
        /// <remarks>Created 9/23/2010.</remarks>
        public class ChatGuildEventArgs : ChatMessageBaseEventArgs
        {
            internal ChatGuildEventArgs(LuaEventArgs from) : base(from)
            {
            }

            /// <summary>Gets the message.</summary>
            /// <value>The message.</value>
            public string Message { get { return Args[0].ToString(); } }

            /// <summary>Gets the author.</summary>
            /// <value>The author.</value>
            public string Author { get { return Args[1].ToString(); } }

            /// <summary>Gets the language.</summary>
            /// <value>The language.</value>
            public string Language { get { return Args[2].ToString(); } }
        }

        #endregion

        #region Nested type: ChatLanguageSpecificEventArgs

        /// <summary>Additional information for chat language specific events. </summary>
        /// <remarks>Created 9/23/2010.</remarks>
        public class ChatLanguageSpecificEventArgs : ChatAuthoredEventArgs
        {
            internal ChatLanguageSpecificEventArgs(LuaEventArgs from) : base(from)
            {
            }

            /// <summary>Gets the language.</summary>
            /// <value>The language.</value>
            public string Language { get { return Args[2].ToString(); } }
        }

        #endregion

        #region Nested type: ChatMessageBaseEventArgs

        /// <summary>Additional information for chat message base events. </summary>
        /// <remarks>Created 9/23/2010.</remarks>
        public class ChatMessageBaseEventArgs : LuaEventArgs
        {
            internal ChatMessageBaseEventArgs(LuaEventArgs from) : base(from.EventName, from.FireTimeStamp, from.Args)
            {
            }
        }

        #endregion

        #region Nested type: ChatMonsterEventArgs

        /// <summary>Additional information for chat monster events. </summary>
        /// <remarks>Created 9/23/2010.</remarks>
        public class ChatMonsterEventArgs : ChatMessageBaseEventArgs
        {
            internal ChatMonsterEventArgs(LuaEventArgs from) : base(from)
            {
            }

            /// <summary>Gets the message.</summary>
            /// <value>The message.</value>
            public string Message { get { return Args[0].ToString(); } }

            /// <summary>Gets the name of the monster.</summary>
            /// <value>The name of the monster.</value>
            public string MonsterName { get { return Args[1].ToString(); } }
        }

        #endregion

        #region Nested type: ChatMonsterSayEventArgs

        /// <summary>Additional information for chat monster say events. </summary>
        /// <remarks>Created 9/23/2010.</remarks>
        public class ChatMonsterSayEventArgs : ChatMonsterEventArgs
        {
            internal ChatMonsterSayEventArgs(LuaEventArgs from) : base(from)
            {
            }

            /// <summary>Gets the language.</summary>
            /// <value>The language.</value>
            public string Language { get { return Args[2].ToString(); } }

            /// <summary>Gets the receiver.</summary>
            /// <value>The receiver.</value>
            public string Receiver { get { return Args[4].ToString(); } }
        }

        #endregion

        #region Nested type: ChatSimpleMessageEventArgs

        /// <summary>Additional information for chat simple message events. </summary>
        /// <remarks>Created 9/23/2010.</remarks>
        public class ChatSimpleMessageEventArgs : ChatMessageBaseEventArgs
        {
            internal ChatSimpleMessageEventArgs(LuaEventArgs from) : base(from)
            {
            }

            /// <summary>Gets the message.</summary>
            /// <value>The message.</value>
            public string Message { get { return Args[0].ToString(); } }
        }

        #endregion

        #region Nested type: ChatWhisperEventArgs

        /// <summary>Additional information for chat whisper events. </summary>
        /// <remarks>Created 9/23/2010.</remarks>
        public class ChatWhisperEventArgs : ChatLanguageSpecificEventArgs
        {
            internal ChatWhisperEventArgs(LuaEventArgs from) : base(from)
            {
            }

            /// <summary>Gets the status.</summary>
            /// <value>The status.</value>
            public string Status { get { return Args[5].ToString(); } }

            /// <summary>Gets the identifier of the message.</summary>
            /// <value>The identifier of the message.</value>
            public int MessageId { get { return (int) (double) Args[6]; } }
        }

        #endregion

        #endregion

        static Chat()
        {
            Lua.Events.AttachEvent("CHANNEL_INVITE_REQUEST", WoWChatExMessageHandler);
            Lua.Events.AttachEvent("CHAT_MSG_ACHIEVEMENT", WoWChatExMessageHandler);
            Lua.Events.AttachEvent("CHAT_MSG_ADDON", WoWChatExMessageHandler);
            Lua.Events.AttachEvent("CHAT_MSG_AFK", WoWChatExMessageHandler);
            Lua.Events.AttachEvent("CHAT_MSG_BATTLEGROUND", WoWChatExMessageHandler);
            Lua.Events.AttachEvent("CHAT_MSG_BATTLEGROUND_LEADER", WoWChatExMessageHandler);
            Lua.Events.AttachEvent("CHAT_MSG_BG_SYSTEM_ALLIANCE", WoWChatExMessageHandler);
            Lua.Events.AttachEvent("CHAT_MSG_BG_SYSTEM_HORDE", WoWChatExMessageHandler);
            Lua.Events.AttachEvent("CHAT_MSG_BG_SYSTEM_NEUTRAL", WoWChatExMessageHandler);
            Lua.Events.AttachEvent("CHAT_MSG_CHANNEL", WoWChatExMessageHandler);
            Lua.Events.AttachEvent("CHAT_MSG_DND", WoWChatExMessageHandler);
            Lua.Events.AttachEvent("CHAT_MSG_EMOTE", WoWChatExMessageHandler);
            Lua.Events.AttachEvent("CHAT_MSG_GUILD", WoWChatExMessageHandler);
            Lua.Events.AttachEvent("CHAT_MSG_MONSTER_EMOTE", WoWChatExMessageHandler);
            Lua.Events.AttachEvent("CHAT_MSG_MONSTER_SAY", WoWChatExMessageHandler);
            Lua.Events.AttachEvent("CHAT_MSG_MONSTER_WHISPER", WoWChatExMessageHandler);
            Lua.Events.AttachEvent("CHAT_MSG_MONSTER_PARTY", WoWChatExMessageHandler);
            Lua.Events.AttachEvent("CHAT_MSG_MONSTER_YELL", WoWChatExMessageHandler);
            Lua.Events.AttachEvent("CHAT_MSG_OFFICER", WoWChatExMessageHandler);
            Lua.Events.AttachEvent("CHAT_MSG_PARTY", WoWChatExMessageHandler);
            Lua.Events.AttachEvent("CHAT_MSG_PARTY_LEADER", WoWChatExMessageHandler);
            Lua.Events.AttachEvent("CHAT_MSG_RAID", WoWChatExMessageHandler);
            Lua.Events.AttachEvent("CHAT_MSG_RAID_BOSS_EMOTE", WoWChatExMessageHandler);
            Lua.Events.AttachEvent("CHAT_MSG_RAID_BOSS_WHISPER", WoWChatExMessageHandler);
            Lua.Events.AttachEvent("CHAT_MSG_RAID_LEADER", WoWChatExMessageHandler);
            Lua.Events.AttachEvent("CHAT_MSG_RAID_WARNING", WoWChatExMessageHandler);
            Lua.Events.AttachEvent("CHAT_MSG_SAY", WoWChatExMessageHandler);
            Lua.Events.AttachEvent("CHAT_MSG_SYSTEM", WoWChatExMessageHandler);
            Lua.Events.AttachEvent("CHAT_MSG_TEXT_EMOTE", WoWChatExMessageHandler);
            Lua.Events.AttachEvent("CHAT_MSG_WHISPER", WoWChatExMessageHandler);
            Lua.Events.AttachEvent("CHAT_MSG_WHISPER_INFORM", WoWChatExMessageHandler);
            Lua.Events.AttachEvent("CHAT_MSG_YELL", WoWChatExMessageHandler);
        }

        /// <summary>Wow chat ex message handler. </summary>
        /// <remarks>Created 9/23/2010.</remarks>
        /// <param name="sender">Source of the event.</param>
        /// <param name="args">Lua event information.</param>
        private static void WoWChatExMessageHandler(object sender, LuaEventArgs args)
        {
            switch (args.EventName)
            {
                case "CHANNEL_INVITE_REQUEST":
                    break;
                case "CHAT_MSG_ACHIEVEMENT":
                    break;
                case "CHAT_MSG_ADDON":
                    if (Addon != null)
                        Addon(new ChatAddonEventArgs(args));
                    break;
                case "CHAT_MSG_AFK":
                    if (Afk != null)
                        Afk(new ChatAuthoredEventArgs(args));
                    break;
                case "CHAT_MSG_BATTLEGROUND":
                    if (Battleground != null)
                        Battleground(new ChatLanguageSpecificEventArgs(args));
                    break;
                case "CHAT_MSG_BATTLEGROUND_LEADER":
                    if (BattlegroundLeader != null)
                        BattlegroundLeader(new ChatLanguageSpecificEventArgs(args));
                    break;
                case "CHAT_MSG_BG_SYSTEM_ALLIANCE":
                    if (AllianceBattleground != null)
                        AllianceBattleground(new ChatSimpleMessageEventArgs(args));
                    break;
                case "CHAT_MSG_BG_SYSTEM_HORDE":
                    if (HordeBattleground != null)
                        HordeBattleground(new ChatSimpleMessageEventArgs(args));
                    break;
                case "CHAT_MSG_BG_SYSTEM_NEUTRAL":
                    if (NeutralBattleground != null)
                        NeutralBattleground(new ChatSimpleMessageEventArgs(args));
                    break;
                case "CHAT_MSG_CHANNEL":
                    if (Channel != null)
                        Channel(new ChatChannelSpecificEventArgs(args));
                    break;
                case "CHAT_MSG_DND":
                    if (Dnd != null)
                        Dnd(new ChatAuthoredEventArgs(args));
                    break;
                case "CHAT_MSG_EMOTE":
                    if (Emote != null)
                        Emote(new ChatAuthoredEventArgs(args));
                    break;
                case "CHAT_MSG_GUILD":
                    if (Guild != null)
                        Guild(new ChatGuildEventArgs(args));
                    break;
                case "CHAT_MSG_MONSTER_EMOTE":
                    if (MonsterEmote != null)
                        MonsterEmote(new ChatMonsterEventArgs(args));
                    break;
                case "CHAT_MSG_MONSTER_SAY":
                    if (MonsterSay != null)
                        MonsterSay(new ChatMonsterSayEventArgs(args));
                    break;
                case "CHAT_MSG_MONSTER_WHISPER":
                    if (MonsterWhisper != null)
                        MonsterWhisper(new ChatMonsterEventArgs(args));
                    break;
                case "CHAT_MSG_MONSTER_PARTY":
                    if (MonsterParty != null)
                        MonsterParty(new ChatMonsterEventArgs(args));
                    break;
                case "CHAT_MSG_MONSTER_YELL":
                    if (MonsterYell != null)
                        MonsterYell(new ChatMonsterEventArgs(args));
                    break;
                case "CHAT_MSG_OFFICER":
                    if (Officer != null)
                        Officer(new ChatLanguageSpecificEventArgs(args));
                    break;
                case "CHAT_MSG_PARTY":
                    if (Party != null)
                        Party(new ChatLanguageSpecificEventArgs(args));
                    break;
                case "CHAT_MSG_PARTY_LEADER":
                    if (PartyLeader != null)
                        PartyLeader(new ChatLanguageSpecificEventArgs(args));
                    break;
                case "CHAT_MSG_RAID":
                    if (Raid != null)
                        Raid(new ChatLanguageSpecificEventArgs(args));
                    break;
                case "CHAT_MSG_RAID_BOSS_EMOTE":
                    if (RaidBossEmote != null)
                        RaidBossEmote(new ChatMonsterEventArgs(args));
                    break;
                case "CHAT_MSG_RAID_BOSS_WHISPER":
                    if (RaidBossWhisper != null)
                        RaidBossWhisper(new ChatMonsterEventArgs(args));
                    break;
                case "CHAT_MSG_RAID_LEADER":
                    if (RaidLeader != null)
                        RaidLeader(new ChatLanguageSpecificEventArgs(args));
                    break;
                case "CHAT_MSG_RAID_WARNING":
                    if (RaidWarning != null)
                        RaidWarning(new ChatLanguageSpecificEventArgs(args));
                    break;
                case "CHAT_MSG_SAY":
                    if (Say != null)
                        Say(new ChatLanguageSpecificEventArgs(args));
                    break;
                case "CHAT_MSG_SYSTEM":
                    if (System != null)
                        System(new ChatSimpleMessageEventArgs(args));
                    break;
                case "CHAT_MSG_TEXT_EMOTE":
                    if (TextEmote != null)
                        TextEmote(new ChatAuthoredEventArgs(args));
                    break;
                case "CHAT_MSG_WHISPER":
                    if (Whisper != null)
                        Whisper(new ChatWhisperEventArgs(args));
                    break;
                case "CHAT_MSG_WHISPER_INFORM":
                    if (WhisperTo != null)
                        WhisperTo(new ChatLanguageSpecificEventArgs(args));
                    break;
                case "CHAT_MSG_YELL":
                    if (Yell != null)
                        Yell(new ChatLanguageSpecificEventArgs(args));
                    break;
            }
        }

        #region Events

        /// <summary> Event queue for all listeners interested in Addon events. </summary>
        public static event ChatMessageHandlerEx<ChatAddonEventArgs> Addon;

        /// <summary> Event queue for all listeners interested in Afk events. </summary>
        public static event ChatMessageHandlerEx<ChatAuthoredEventArgs> Afk;

        /// <summary> Event queue for all listeners interested in Dnd events. </summary>
        public static event ChatMessageHandlerEx<ChatAuthoredEventArgs> Dnd;

        /// <summary> Event queue for all listeners interested in TextEmote events. </summary>
        public static event ChatMessageHandlerEx<ChatAuthoredEventArgs> TextEmote;

        /// <summary> Event queue for all listeners interested in Emote events. </summary>
        public static event ChatMessageHandlerEx<ChatAuthoredEventArgs> Emote;

        /// <summary> Event queue for all listeners interested in Whisper events. </summary>
        public static event ChatMessageHandlerEx<ChatWhisperEventArgs> Whisper;

        /// <summary> Event queue for all listeners interested in WhisperTo events. </summary>
        public static event ChatMessageHandlerEx<ChatLanguageSpecificEventArgs> WhisperTo;

        /// <summary> Event queue for all listeners interested in Battleground events. </summary>
        public static event ChatMessageHandlerEx<ChatLanguageSpecificEventArgs> Battleground;

        /// <summary> Event queue for all listeners interested in Yell events. </summary>
        public static event ChatMessageHandlerEx<ChatLanguageSpecificEventArgs> Yell;

        /// <summary> Event queue for all listeners interested in BattlegroundLeader events. </summary>
        public static event ChatMessageHandlerEx<ChatLanguageSpecificEventArgs> BattlegroundLeader;

        /// <summary> Event queue for all listeners interested in AllianceBattleground events. </summary>
        public static event ChatMessageHandlerEx<ChatSimpleMessageEventArgs> AllianceBattleground;

        /// <summary> Event queue for all listeners interested in HordeBattleground events. </summary>
        public static event ChatMessageHandlerEx<ChatSimpleMessageEventArgs> HordeBattleground;

        /// <summary> Event queue for all listeners interested in NeutralBattleground events. </summary>
        public static event ChatMessageHandlerEx<ChatSimpleMessageEventArgs> NeutralBattleground;

        /// <summary> Event queue for all listeners interested in System events. </summary>
        public static event ChatMessageHandlerEx<ChatSimpleMessageEventArgs> System;

        /// <summary> Event queue for all listeners interested in Channel events. </summary>
        public static event ChatMessageHandlerEx<ChatChannelSpecificEventArgs> Channel;

        /// <summary> Event queue for all listeners interested in Guild events. </summary>
        public static event ChatMessageHandlerEx<ChatGuildEventArgs> Guild;

        /// <summary> Event queue for all listeners interested in MonsterEmote events. </summary>
        public static event ChatMessageHandlerEx<ChatMonsterEventArgs> MonsterEmote;

        /// <summary> Event queue for all listeners interested in MonsterSay events. </summary>
        public static event ChatMessageHandlerEx<ChatMonsterSayEventArgs> MonsterSay;

        /// <summary> Event queue for all listeners interested in MonsterYell events. </summary>
        public static event ChatMessageHandlerEx<ChatMonsterEventArgs> MonsterYell;

        /// <summary> Event queue for all listeners interested in MonsterWhisper events. </summary>
        public static event ChatMessageHandlerEx<ChatMonsterEventArgs> MonsterWhisper;

        /// <summary> Event queue for all listeners interested in MonsterParty events. </summary>
        public static event ChatMessageHandlerEx<ChatMonsterEventArgs> MonsterParty;

        /// <summary> Event queue for all listeners interested in RaidBossEmote events. </summary>
        public static event ChatMessageHandlerEx<ChatMonsterEventArgs> RaidBossEmote;

        /// <summary> Event queue for all listeners interested in RaidBossWhisper events. </summary>
        public static event ChatMessageHandlerEx<ChatMonsterEventArgs> RaidBossWhisper;

        /// <summary> Event queue for all listeners interested in Officer events. </summary>
        public static event ChatMessageHandlerEx<ChatLanguageSpecificEventArgs> Officer;

        /// <summary> Event queue for all listeners interested in Party events. </summary>
        public static event ChatMessageHandlerEx<ChatLanguageSpecificEventArgs> Party;

        /// <summary> Event queue for all listeners interested in PartyLeader events. </summary>
        public static event ChatMessageHandlerEx<ChatLanguageSpecificEventArgs> PartyLeader;

        /// <summary> Event queue for all listeners interested in Raid events. </summary>
        public static event ChatMessageHandlerEx<ChatLanguageSpecificEventArgs> Raid;

        /// <summary> Event queue for all listeners interested in RaidLeader events. </summary>
        public static event ChatMessageHandlerEx<ChatLanguageSpecificEventArgs> RaidLeader;

        /// <summary> Event queue for all listeners interested in RaidWarning events. </summary>
        public static event ChatMessageHandlerEx<ChatLanguageSpecificEventArgs> RaidWarning;

        /// <summary> Event queue for all listeners interested in Say events. </summary>
        public static event ChatMessageHandlerEx<ChatLanguageSpecificEventArgs> Say;

        #endregion
    }
}
