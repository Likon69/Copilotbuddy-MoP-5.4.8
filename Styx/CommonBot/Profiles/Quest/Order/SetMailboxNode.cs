using System.Collections.Generic;
using System.Xml.Linq;
using Styx.Resources;

namespace Styx.CommonBot.Profiles.Quest.Order
{
    public class SetMailboxNode : OrderNode
    {
        public SetMailboxNode(List<Mailbox> mailboxes) : base(OrderNodeType.SetMailbox)
        {
            Mailboxes = mailboxes;
        }

        public SetMailboxNode(Mailbox mailbox) : this(new List<Mailbox> { mailbox })
        {

        }

        public List<Mailbox> Mailboxes { get; private set; }

        public static new SetMailboxNode FromXml(XElement element)
        {
            XElement subElement = ProfileHelper.FindElement(element, true, "mailbox", "mailboxes");

            if (subElement == null)
                throw new ProfileMissingElementException("Mailbox", element);

            if (subElement.Name.ToString().ToLowerInvariant() == "mailbox")
            {
                Mailbox box;
                try
                {
                    box = new Mailbox(subElement);
                }
                catch (ProfileException ex)
                {
                    throw new ProfileException(StyxResources.Could_not_parse_SetMailbox_node_, ex);
                }

                return new SetMailboxNode(box);
            }

            var mailboxes = new List<Mailbox>();
            foreach (XElement mailboxElem in ProfileHelper.FindElements(subElement, true, "mailbox"))
            {
                try
                {
                    mailboxes.Add(new Mailbox(mailboxElem));
                }
                catch (ProfileException ex)
                {
                    throw new ProfileException(StyxResources.Could_not_parse_SetMailbox_node_, ex);
                }
            }

            return new SetMailboxNode(mailboxes);
        }

        public override string ToString()
        {
            return string.Format("[SetMailboxNode Mailboxes: {0}]", string.Join(", ", Mailboxes.ConvertAll(mb => mb.ToString()).ToArray()));
        }
    }
}
