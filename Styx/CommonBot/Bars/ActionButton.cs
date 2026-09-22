using System;
using System.Collections.Generic;
using Styx.WoWInternals;

namespace Styx.CommonBot.Bars
{
    public class ActionButton
    {
        internal ActionButton(int slot, int index, string idStr, ActionButtonType type, ActionButtonSubType subType)
        {
            int parsedId;
            if (int.TryParse(idStr, out parsedId))
            {
                this.Id = parsedId;
            }
            this.IdString = idStr;
            this.Slot = slot;
            this.Index = index;
            this.Type = type;
            this.SubType = subType;
            this.UpdateState();
        }

        public int Index { get; private set; }

        public int Slot { get; private set; }

        public bool CanUse
        {
            get
            {
                return this.IsEnabled && !this.Cooldown;
            }
        }

        public virtual bool IsEnabled { get; private set; }

        public TimeSpan CoolDownLeft
        {
            get
            {
                long elapsedMilliseconds = (long)Environment.TickCount - this.cooldownStartMilliseconds;
                if (this.cooldownStartMilliseconds != 0L && elapsedMilliseconds < (long)this.cooldownDurationMilliseconds)
                {
                    return TimeSpan.FromMilliseconds((double)((long)this.cooldownDurationMilliseconds - elapsedMilliseconds));
                }
                return TimeSpan.Zero;
            }
        }

        public bool Cooldown
        {
            get
            {
                return this.CoolDownLeft != TimeSpan.Zero && this.ChargeNum == 0;
            }
        }

        public string IdString { get; private set; }

        public int Id { get; private set; }

        public virtual string Name
        {
            get
            {
                return string.Format("[{0}]", this.Slot);
            }
        }

        public int ChargeNum { get; private set; }

        public int MaxChargeNum { get; private set; }

        public ActionButtonType Type { get; private set; }

        public ActionButtonSubType SubType { get; private set; }

        private void UpdateState()
        {
            using (StyxWoW.Memory.AcquireFrame())
            {
                string lua = string.Format("isUsable, notEnoughMana = IsUsableAction({0}) if isUsable and not notEnoughMana then return 1 end return nil", this.Slot);
                this.IsEnabled = Styx.WoWInternals.Lua.GetReturnVal<bool>(lua, 0U);
                List<string> returnValues = Styx.WoWInternals.Lua.GetReturnValues(string.Format("return GetActionCooldown({0})", this.Slot));
                this.cooldownStartMilliseconds = (long)(float.Parse(returnValues[0]) * 1000f);
                if (int.TryParse(returnValues[1], out this.cooldownDurationMilliseconds))
                {
                    this.cooldownDurationMilliseconds *= 1000;
                }
                else
                {
                    this.cooldownDurationMilliseconds = 0;
                }
                this.ChargeNum = int.Parse(returnValues[3]);
                this.MaxChargeNum = int.Parse(returnValues[4]);
            }
        }

        public void Use()
        {
            Styx.WoWInternals.Lua.DoString("UseAction({0})", new object[] { this.Slot });
        }

        public override string ToString()
        {
            return string.Format("Name: {0}, IdString: {1}, Type: {2}, SubType: {3} Index: {4}, Slot: {5}, IsEnabled: {6}, ChargeNum: {7}, MaxChargeNum: {8}, Cooldown: {9}", new object[] { this.Name, this.IdString, this.Type, this.SubType, this.Index, this.Slot, this.IsEnabled, this.ChargeNum, this.MaxChargeNum, this.Cooldown });
        }

        private int cooldownDurationMilliseconds;

        private long cooldownStartMilliseconds;
    }
}
