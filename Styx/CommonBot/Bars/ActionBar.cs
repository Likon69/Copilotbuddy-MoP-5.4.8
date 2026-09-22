using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Styx.Helpers;
using Styx.WoWInternals;

namespace Styx.CommonBot.Bars
{
    public class ActionBar
    {
        private ActionBar(int page, ActionBarType type)
        {
            this.Page = page;
            this.Type = type;
        }

        public int Page { get; private set; }

        public ActionBarType Type { get; private set; }

        [JetBrains.Annotations.NotNull]
        public IReadOnlyList<ActionButton> Buttons
        {
            get
            {
                Styx.Helpers.PerFrameCachedValue<IReadOnlyList<ActionButton>> cachedButtons;
                if ((cachedButtons = this.buttons) == null)
                {
                    cachedButtons = (this.buttons = new Styx.Helpers.PerFrameCachedValue<IReadOnlyList<ActionButton>>(new Func<IReadOnlyList<ActionButton>>(this.GetButtons)));
                }
                return cachedButtons.Value;
            }
        }

        private IEnumerable<ActionButton> EnumerateButtons()
        {
            using (StyxWoW.Memory.AcquireFrame())
            {
                int pageOffset = ActionBar.GetPageOffset(this.Page);
                int buttonIndex = 1;
                while ((long)buttonIndex <= 12L)
                {
                    int slot = buttonIndex + pageOffset;
                    List<string> returnValues = Styx.WoWInternals.Lua.GetReturnValues(string.Format("return GetActionInfo({0})", slot));
                    if (returnValues.Count >= 2)
                    {
                        ActionButtonType actionButtonType = ActionBar.ParseActionButtonType(returnValues[0]);
                        string idString = returnValues[1];
                        if (!ActionBar.RequiresIdentifier(actionButtonType) || (!string.IsNullOrEmpty(idString) && !(idString == "0")))
                        {
                            ActionButtonSubType actionButtonSubType = ((returnValues.Count < 3) ? ActionButtonSubType.Unknown : ActionBar.ParseActionButtonSubType(returnValues[2]));
                            ActionButton actionButton = ActionBar.CreateActionButton(slot, buttonIndex, idString, actionButtonType, actionButtonSubType);
                            yield return actionButton;
                        }
                    }
                    buttonIndex++;
                }
            }
            yield break;
        }

        public bool IsActive
        {
            get
            {
                Styx.Helpers.PerFrameCachedValue<bool> cachedIsActive;
                if ((cachedIsActive = this.isActive) == null)
                {
                    cachedIsActive = (this.isActive = new Styx.Helpers.PerFrameCachedValue<bool>(new Func<bool>(this.GetIsActive)));
                }
                return cachedIsActive;
            }
        }

        private static int ExtraBarPage
        {
            get
            {
                int? cachedPage = ActionBar.extraBarPage;
                if (cachedPage == null)
                {
                    int? page = (ActionBar.extraBarPage = new int?(Styx.WoWInternals.Lua.GetReturnVal<int>("return GetExtraBarIndex()", 0U)));
                    return page.Value;
                }
                return cachedPage.GetValueOrDefault();
            }
        }

        private static int OverrideBarIndex
        {
            get
            {
                int? cachedPage = ActionBar.overrideBarIndex;
                if (cachedPage == null)
                {
                    int? page = (ActionBar.overrideBarIndex = new int?(Styx.WoWInternals.Lua.GetReturnVal<int>("return GetOverrideBarIndex()", 0U)));
                    return page.Value;
                }
                return cachedPage.GetValueOrDefault();
            }
        }

        private static int VehicleBarPage
        {
            get
            {
                int? cachedPage = ActionBar.vehicleBarPage;
                if (cachedPage == null)
                {
                    int? page = (ActionBar.vehicleBarPage = new int?(Styx.WoWInternals.Lua.GetReturnVal<int>("return GetVehicleBarIndex()", 0U)));
                    return page.Value;
                }
                return cachedPage.GetValueOrDefault();
            }
        }

        private static int TempShapeshiftBarPage
        {
            get
            {
                int? cachedPage = ActionBar.tempShapeshiftBarPage;
                if (cachedPage == null)
                {
                    int? page = (ActionBar.tempShapeshiftBarPage = new int?(Styx.WoWInternals.Lua.GetReturnVal<int>("return GetTempShapeshiftBarIndex()", 0U)));
                    return page.Value;
                }
                return cachedPage.GetValueOrDefault();
            }
        }

        [JetBrains.Annotations.NotNull]
        public static ActionBar Active
        {
            get
            {
                Styx.Helpers.PerFrameCachedValue<ActionBar> cachedActiveBar;
                if ((cachedActiveBar = ActionBar.active) == null)
                {
                    cachedActiveBar = (ActionBar.active = new Styx.Helpers.PerFrameCachedValue<ActionBar>(new Func<ActionBar>(ActionBar.CreateActiveActionBar)));
                }
                return cachedActiveBar;
            }
        }

        [JetBrains.Annotations.NotNull]
        public static ActionBar Extra
        {
            get
            {
                Styx.Helpers.PerFrameCachedValue<ActionBar> cachedExtraBar;
                if ((cachedExtraBar = ActionBar.extra) == null)
                {
                    cachedExtraBar = (ActionBar.extra = new Styx.Helpers.PerFrameCachedValue<ActionBar>(new Func<ActionBar>(ActionBar.CreateExtraActionBar)));
                }
                return cachedExtraBar;
            }
        }

        private static int ActiveActionBarPage
        {
            get
            {
                Styx.Helpers.PerFrameCachedValue<int> cachedActivePage;
                if ((cachedActivePage = ActionBar.activeActionBarPage) == null)
                {
                    cachedActivePage = (ActionBar.activeActionBarPage = new Styx.Helpers.PerFrameCachedValue<int>(new Func<int>(ActionBar.GetActiveActionBarPage)));
                }
                return cachedActivePage;
            }
        }

        private static ActionBarType GetActionBarType(int page)
        {
            if ((long)page < 1L)
            {
                throw new ArgumentException("Page can't be lower than 1");
            }
            if ((long)page <= 6L)
            {
                return ActionBarType.Normal;
            }
            if ((long)page <= 10L)
            {
                return ActionBarType.Bonus;
            }
            if (page == ActionBar.ExtraBarPage)
            {
                return ActionBarType.Extra;
            }
            if (page == ActionBar.OverrideBarIndex)
            {
                return ActionBarType.Override;
            }
            if (page == ActionBar.VehicleBarPage)
            {
                return ActionBarType.Vehicle;
            }
            if (page == ActionBar.TempShapeshiftBarPage)
            {
                return ActionBarType.TempShapeshift;
            }
            return ActionBarType.Unknown;
        }

        private static int GetPageOffset(int page)
        {
            return (int)((long)(page - 1) * 12L);
        }

        private static ActionButton CreateActionButton(int slot, int index, string idString, ActionButtonType actionButtonType, ActionButtonSubType actionButtonSubType)
        {
            if (actionButtonType == ActionButtonType.Spell)
            {
                return new SpellActionButton(slot, index, idString, actionButtonType, actionButtonSubType);
            }
            return new ActionButton(slot, index, idString, actionButtonType, actionButtonSubType);
        }

        private static ActionButtonType ParseActionButtonType(string value)
        {
            if (value == null)
            {
                return ActionButtonType.Unknown;
            }
            string normalizedValue;
            if ((normalizedValue = value.ToLowerInvariant()) != null)
            {
                if (normalizedValue == "companion")
                {
                    return ActionButtonType.Companion;
                }
                if (normalizedValue == "equipmentset")
                {
                    return ActionButtonType.EquipmentSet;
                }
                if (normalizedValue == "flyout")
                {
                    return ActionButtonType.Flyout;
                }
                if (normalizedValue == "item")
                {
                    return ActionButtonType.Item;
                }
                if (normalizedValue == "macro")
                {
                    return ActionButtonType.Macro;
                }
                if (normalizedValue == "spell")
                {
                    return ActionButtonType.Spell;
                }
            }
            return ActionButtonType.Unknown;
        }

        private static ActionButtonSubType ParseActionButtonSubType(string value)
        {
            if (value == null)
            {
                return ActionButtonSubType.Unknown;
            }
            string normalizedValue;
            if ((normalizedValue = value.ToLowerInvariant()) != null)
            {
                if (normalizedValue == "mount")
                {
                    return ActionButtonSubType.Mount;
                }
                if (normalizedValue == "critter")
                {
                    return ActionButtonSubType.Critter;
                }
                if (normalizedValue == "pet")
                {
                    return ActionButtonSubType.Pet;
                }
                if (normalizedValue == "spell")
                {
                    return ActionButtonSubType.Spell;
                }
            }
            return ActionButtonSubType.Unknown;
        }

        private static bool UsesActiveActionBarPage(ActionBarType actionBarType)
        {
            switch (actionBarType)
            {
            case ActionBarType.Bonus:
            case ActionBarType.Vehicle:
            case ActionBarType.Override:
            case ActionBarType.TempShapeshift:
                return true;
            default:
                return false;
            }
        }

        private static bool RequiresIdentifier(ActionButtonType actionButtonType)
        {
            return actionButtonType != ActionButtonType.SummonPet;
        }

        private static bool UsesNumericIdentifier(ActionButtonType actionButtonType)
        {
            if (actionButtonType != ActionButtonType.EquipmentSet)
            {
                if (actionButtonType != ActionButtonType.SummonPet)
                {
                    return true;
                }
            }
            return false;
        }

        private IReadOnlyList<ActionButton> GetButtons()
        {
            return this.EnumerateButtons().ToList<ActionButton>().AsReadOnly();
        }

        private bool GetIsActive()
        {
            if (this.Type != ActionBarType.Normal && !ActionBar.UsesActiveActionBarPage(this.Type))
            {
                ActionBarType type = this.Type;
                return type == ActionBarType.Extra && Styx.WoWInternals.Lua.GetReturnVal<bool>("return HasExtraActionBar()", 0U);
            }
            return ActionBar.ActiveActionBarPage == this.Page;
        }

        private static ActionBar CreateActiveActionBar()
        {
            return new ActionBar(ActionBar.ActiveActionBarPage, ActionBar.GetActionBarType(ActionBar.ActiveActionBarPage));
        }

        private static ActionBar CreateExtraActionBar()
        {
            return new ActionBar(ActionBar.ExtraBarPage, ActionBarType.Extra);
        }

        private static int GetActiveActionBarPage()
        {
            int page;
            try
            {
                List<string> returnValues = Styx.WoWInternals.Lua.GetReturnValues("if HasVehicleActionBar() then return GetVehicleBarIndex() elseif HasOverrideActionBar() then return GetOverrideBarIndex() elseif HasTempShapeshiftActionBar() then return GetTempShapeshiftBarIndex() elseif HasBonusActionBar() and GetActionBarPage() == 1 then return GetBonusBarIndex() else return GetActionBarPage() end");
                page = int.Parse(returnValues[0]);
            }
            catch (Exception)
            {
                page = 1;
            }
            return page;
        }

        private const uint FirstActionBarPage = 1U;

        private const uint LastNormalActionBarPage = 6U;

        private const uint FirstBonusActionBarPage = 7U;

        private const uint LastBonusActionBarPage = 10U;

        private const uint ButtonsPerBar = 12U;

        private Styx.Helpers.PerFrameCachedValue<IReadOnlyList<ActionButton>> buttons;

        private Styx.Helpers.PerFrameCachedValue<bool> isActive;

        private static int? extraBarPage;

        private static int? overrideBarIndex;

        private static int? vehicleBarPage;

        private static int? tempShapeshiftBarPage;

        private static Styx.Helpers.PerFrameCachedValue<ActionBar> extra;

        private static Styx.Helpers.PerFrameCachedValue<ActionBar> active;

        private static Styx.Helpers.PerFrameCachedValue<int> activeActionBarPage;
    }
}
