using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Buddy.Coroutines;
using Styx.Common;
using Styx.Resources;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace Styx.CommonBot.Frames
{
    public class MailFrame : Frame
    {
        public static readonly MailFrame Instance = new MailFrame();

        public MailFrame() : base("MailFrame") { }

        public int MailCount => Lua.GetReturnVal<int>("return GetInboxNumItems()", 0U);

        public bool HasNewMail
        {
            get
            {
                Lua.DoString("CheckInbox()", "WoW.lua");
                return Lua.GetReturnVal<bool>("return HasNewMail()", 0U);
            }
        }

        public bool GetCodMail { get; set; }
        public int GetCodMailMax { get; set; }

        public IEnumerable<InboxMailItem> GetAllMails()
        {
            for (var index = MailCount; index >= 1; index--)
                yield return new InboxMailItem(Lua.GetReturnValues("return GetInboxHeaderInfo(" + index + ")"), index);
        }

        public void DeleteMail(int mailIndex) => Lua.DoString(string.Format("DeleteInboxItem({0})", mailIndex), "Economist.lua");
        public void ReturnMail(int mailIndex) => Lua.DoString(string.Format("ReturnInboxItem({0})", mailIndex), "Economist.lua");

        public void GetMailAttachments(int mailIndex)
        {
            new InboxMailItem(Lua.GetReturnValues("return GetInboxHeaderInfo(" + mailIndex + ")"), mailIndex).TakeAttachedItems();
        }

        public void OpenAllMail()
        {
            if (!IsVisible) return;
            foreach (var mail in GetAllMails().Where(mail => mail != null && mail.CODAmount <= 0))
            {
                Logging.WriteDiagnostic("Opening mail: " + mail);
                mail.TakeAttachedItems();
            }
            if (!GetCodMail) return;
            foreach (var mail in GetAllMails().Where(mail => mail != null && mail.CODAmount <= GetCodMailMax))
            {
                Logging.WriteDiagnostic("Opening COD mail: " + mail);
                mail.TakeAttachedItems();
            }
        }

        public void OpenAllMailFrom(string senderName)
        {
            if (!IsVisible) return;
            Lua.DoString("CheckInbox()", "WoW.lua");
            foreach (var mail in GetAllMails().Where(mail => mail.Sender == senderName))
                mail?.TakeAttachedItems();
        }

        public void SwitchToSendMailTab() => Lua.DoString("MailFrameTab2:Click()", "WoW.lua");

        public ulong[] SendMailItemGuids
        {
            get
            {
                var guids = new ulong[16];
                for (var i = 0; i < guids.Length; i++)
                    guids[i] = StyxWoW.Memory.Read<ulong>(StyxWoW.Offsets.Statics.SendMailItemGuids + i * 24);
                return guids;
            }
        }

        public WoWItem[] SendMailItems => SendMailItemGuids
            .Select(ObjectManager.GetObjectByGuid<WoWItem>)
            .Where(item => item != null && item.IsValid)
            .ToArray();

        public void SendMail(string recipient, string subject, string body, int copper, params WoWItem[] attachmentItems)
        {
            if (!IsVisible) return;
            if (attachmentItems != null && attachmentItems.Length > 12)
                throw new ArgumentException(StyxResources.You_can_not_attach_more_than_12_items_, "attachmentItems");

            PrepareMail(recipient, subject, copper);
            if (attachmentItems != null && attachmentItems.Length > 0)
            {
                foreach (var item in attachmentItems) item.UseContainerItem();
                if (SendMailItems.Length != attachmentItems.Length)
                    Logging.Write(StyxResources.Something_is_messed_up__we_didn_t_attach_all_of_our_items_);
            }
            Lua.DoString(string.Format("SendMail(\"{0}\", \"{1}\", \"{2}\")", recipient, subject, body), "_main.lua");
        }

        private bool PrepareMail(string recipient, string subject, int copper)
        {
            if (!IsVisible) return false;
            Lua.DoString("ClearSendMail()", "WoW.lua");
            SwitchToSendMailTab();
            Lua.DoString(string.Format("SendMailNameEditBox:SetText('{0}')", recipient), "WoW.lua");
            Lua.DoString(string.Format("SendMailSubjectEditBox:SetText('{0}')", subject), "WoW.lua");
            if (copper >= 1) Lua.DoString(string.Format("SetSendMailMoney({0})", copper), "Mail.lua");
            return true;
        }

        public async Task<bool> SendMailWithManyAttachmentsCoroutine(string recipient, WoWItem[] attachments)
        {
            for (var batchStart = 0; batchStart < attachments.Length; batchStart += 12)
            {
                if (!PrepareMail(recipient, attachments[batchStart].Name, 0))
                {
                    Logging.WriteDiagnostic("SendMailWithManyAttachmentsCoroutine could not prepare mail!");
                    return false;
                }

                await Coroutine.Sleep(200);
                for (var i = batchStart; i < Math.Min(attachments.Length, batchStart + 12); i++)
                {
                    attachments[i].UseContainerItem();
                    await Coroutine.Sleep(100);
                }

                await Coroutine.Sleep(1000);
                Lua.DoString("SendMailMailButton:Click()");
                if (!await Coroutine.Wait(5000, () => !attachments[batchStart].IsValid))
                {
                    Logging.WriteDiagnostic("Item did not disappear after mailing - waiting 5 seconds and continuing mail cycle");
                    await Coroutine.Sleep(5000);
                }
                else
                {
                    await Coroutine.Sleep(1000);
                }
            }
            return true;
        }

        [Obsolete("This API is outdated and should not be used. Use SendMailWithManyAttachmentsCoroutine instead.")]
        public void SendMailWithManyAttachments(string recipient, int copper, params WoWItem[] attachments)
        {
            var rounds = (int)Math.Ceiling(attachments.Length / 11f);
            var remaining = attachments.ToList();
            for (var i = 0; i < rounds; i++)
            {
                var current = remaining.Take(11).ToList();
                if (remaining.Count >= 11) remaining.RemoveRange(0, 11);
                var bagItems = StyxWoW.Me.BagItems;
                var itemCount = bagItems.Count;
                SendMail(recipient, current.Count > 0 ? current[0].Name : "test", "", copper, current.ToArray());
                var started = Environment.TickCount;
                using (StyxWoW.Memory.ReleaseFrame(true))
                {
                    while (bagItems.Count >= itemCount && Environment.TickCount - started < 10000)
                    {
                        Pulsator.Pulse(TreeRoot.Current.PulseFlags);
                        bagItems = StyxWoW.Me.BagItems;
                        StyxWoW.Sleep(250);
                    }
                }
            }
        }

        public override void Hide() => Lua.DoString("CloseMail()", "WoW.lua");
        public void Close() => Hide();

        public class InboxMailItem
        {
            internal InboxMailItem(List<string> args, int mailIndex)
            {
                PackageIcon = args[0];
                StationaryIcon = args[1];
                Sender = args[2];
                Subject = args[3];
                Copper = args[4] != "nil" ? int.Parse(args[4]) : 0;
                CODAmount = args[5] != "nil" ? int.Parse(args[5]) : 0;
                DaysLeft = args[6] != "nil" ? float.Parse(args[6]) : 0f;
                if (args.Count > 7) ItemCount = args[7] != "nil" ? int.Parse(args[7]) : 0;
                if (args.Count > 8) WasRead = args[8] != "nil";
                if (args.Count > 9) WasReturned = args[9] != "nil";
                if (args.Count > 10) CopyCreated = args[10] != "nil";
                if (args.Count > 11) CanReply = args[11] != "nil";
                if (args.Count > 12) IsGameMaster = args[12] != "nil";
                if (args.Count > 13) ItemQuantity = args[13] != "nil" ? int.Parse(args[13]) : 0;
                Index = mailIndex;
            }

            public int Index { get; set; }
            public string PackageIcon { get; set; }
            public string StationaryIcon { get; set; }
            public string Sender { get; set; }
            public string Subject { get; set; }
            public int Copper { get; set; }
            public int CODAmount { get; set; }
            public float DaysLeft { get; set; }
            public int ItemCount { get; set; }
            public bool WasRead { get; set; }
            public bool WasReturned { get; set; }
            public bool CopyCreated { get; set; }
            public bool CanReply { get; set; }
            public bool IsGameMaster { get; set; }
            public int ItemQuantity { get; set; }

            public void TakeAttachedItems()
            {
                Lua.DoString("AutoLootMailItem(" + Index + ")", "WoW.lua");
                StyxWoW.Sleep(1000);
            }

            public override string ToString()
            {
                return string.Format("Index: {0}, PackageIcon: {1}, StationaryIcon: {2}, Sender: {3}, Subject: {4}, Copper: {5}, CODAmount: {6}, DaysLeft: {7}, ItemCount: {8}, WasRead: {9}, WasReturned: {10}, CopyCreated: {11}, CanReply: {12}, IsGameMaster: {13}, ItemQuantity: {14}",
                    Index, PackageIcon, StationaryIcon, Sender, Subject, Copper, CODAmount, DaysLeft, ItemCount,
                    WasRead, WasReturned, CopyCreated, CanReply, IsGameMaster, ItemQuantity);
            }
        }
    }
}
