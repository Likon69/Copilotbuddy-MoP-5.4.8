using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Styx.Common;

namespace Styx.CommonBot.Profiles
{
    public class MailboxManager
    {
        /// <summary>
        /// List of mailboxes you can set to 'override' the current mailboxes defined by the profile.
        /// </summary>
        public List<Mailbox> ForcedMailboxes { get; set; }

        public List<Mailbox> AllMailboxes { get; private set; }

        public List<Mailbox> Blacklist { get; private set; }

        private readonly List<Mailbox> _mailboxes = new List<Mailbox>();
        public List<Mailbox> Mailboxes
        {
            get { return _mailboxes.Where(mailbox => !Blacklist.Contains(mailbox)).ToList(); }
        }

        public MailboxManager()
        {
            ForcedMailboxes = new List<Mailbox>();
            AllMailboxes = new List<Mailbox>();
            Blacklist = new List<Mailbox>();
        }

        public MailboxManager(XElement xml)
        {
            AllMailboxes = new List<Mailbox>();
            Blacklist = new List<Mailbox>();
            ForcedMailboxes = new List<Mailbox>();

            List<XElement> mailboxes = xml.Elements().ToList();
            foreach (XElement mailbox in mailboxes)
            {
                try
                {
                    if (mailbox.Name.ToString().ToLowerInvariant() == "mailbox")
                    {
                        var box = new Mailbox(mailbox);
                        AllMailboxes.Add(box);
                    }
                    else
                    {
                        throw new ProfileUnknownElementException(mailbox, "Mailbox");
                    }
                }
                catch (ProfileException ex)
                {
                    Logging.WriteException(ex);
                }
            }

            _mailboxes = AllMailboxes;
        }

        public Mailbox GetClosestMailbox()
        {
            return GetClosestMailbox(StyxWoW.Me.Location);
        }

        public Mailbox GetClosestMailbox(WoWPoint location)
        {
            Mailbox closest = null;
            float closestDist = float.MaxValue;

            var mailboxes = ForcedMailboxes != null && ForcedMailboxes.Count > 0
                                ? ForcedMailboxes
                                : Mailboxes;

            foreach (Mailbox mailbox in mailboxes.Where(mailbox => mailbox.UsableWhen == null || mailbox.UsableWhen()))
            {
                float dist = location.DistanceSqr(mailbox.Location);
                if (dist >= closestDist)
                    continue;
                closestDist = dist;
                closest = mailbox;
            }

            return closest;
        }
    }
}
