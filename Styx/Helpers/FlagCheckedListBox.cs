using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace Styx.Helpers
{
    public class FlagCheckedListBox : CheckedListBox
    {
        public FlagCheckedListBox()
        {
            this.InitializeComponent();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && this.components != null)
            {
                this.components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            base.CheckOnClick = true;
        }

        public FlagCheckedListBoxItem Add(int v, string c)
        {
            FlagCheckedListBoxItem item = new FlagCheckedListBoxItem(v, c);
            base.Items.Add(item);
            return item;
        }

        public FlagCheckedListBoxItem Add(FlagCheckedListBoxItem item)
        {
            base.Items.Add(item);
            return item;
        }

        protected override void OnItemCheck(ItemCheckEventArgs e)
        {
            base.OnItemCheck(e);
            if (this.updatingCheckedItems)
            {
                return;
            }
            FlagCheckedListBoxItem item = base.Items[e.Index] as FlagCheckedListBoxItem;
            this.UpdateCheckedItems(item, e.NewValue);
        }

        protected void UpdateCheckedItems(int value)
        {
            this.updatingCheckedItems = true;
            for (int i = 0; i < base.Items.Count; i++)
            {
                FlagCheckedListBoxItem item = base.Items[i] as FlagCheckedListBoxItem;
                if (item.value == 0)
                {
                    base.SetItemChecked(i, value == 0);
                }
                else if ((item.value & value) == item.value && item.value != 0)
                {
                    base.SetItemChecked(i, true);
                }
                else
                {
                    base.SetItemChecked(i, false);
                }
            }
            this.updatingCheckedItems = false;
        }

        protected void UpdateCheckedItems(FlagCheckedListBoxItem composite, CheckState cs)
        {
            if (composite.value == 0)
            {
                this.UpdateCheckedItems(0);
            }
            int currentValue = 0;
            for (int i = 0; i < base.Items.Count; i++)
            {
                FlagCheckedListBoxItem item = base.Items[i] as FlagCheckedListBoxItem;
                if (base.GetItemChecked(i))
                {
                    currentValue |= item.value;
                }
            }
            if (cs == CheckState.Unchecked)
            {
                currentValue &= ~composite.value;
            }
            else
            {
                currentValue |= composite.value;
            }
            this.UpdateCheckedItems(currentValue);
        }

        public int GetCurrentValue()
        {
            int currentValue = 0;
            for (int i = 0; i < base.Items.Count; i++)
            {
                FlagCheckedListBoxItem item = base.Items[i] as FlagCheckedListBoxItem;
                if (base.GetItemChecked(i))
                {
                    currentValue |= item.value;
                }
            }
            return currentValue;
        }

        private void PopulateItems()
        {
            foreach (string name in Enum.GetNames(this.enumType))
            {
                object enumValue = Enum.Parse(this.enumType, name);
                int value = (int)Convert.ChangeType(enumValue, typeof(int));
                this.Add(value, name);
            }
        }

        private void UpdateCheckedItemsFromEnumValue()
        {
            int value = (int)Convert.ChangeType(this.enumValue, typeof(int));
            this.UpdateCheckedItems(value);
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Enum EnumValue
        {
            get
            {
                object value = Enum.ToObject(this.enumType, this.GetCurrentValue());
                return (Enum)value;
            }
            set
            {
                base.Items.Clear();
                this.enumValue = value;
                this.enumType = value.GetType();
                this.PopulateItems();
                this.UpdateCheckedItemsFromEnumValue();
            }
        }

        private Container components;

        private bool updatingCheckedItems;

        private Type enumType;

        private Enum enumValue;
    }
}
