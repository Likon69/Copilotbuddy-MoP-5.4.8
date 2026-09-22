using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace Styx.Helpers
{
    public class FlagEnumUIEditor : UITypeEditor
    {
        public FlagEnumUIEditor()
        {
            this.flagCheckedListBox = new FlagCheckedListBox();
            this.flagCheckedListBox.BorderStyle = BorderStyle.None;
        }

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            if (context != null && context.Instance != null && provider != null)
            {
                IWindowsFormsEditorService windowsFormsEditorService = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
                if (windowsFormsEditorService != null)
                {
                    Enum enumValue = (Enum)Convert.ChangeType(value, context.PropertyDescriptor.PropertyType);
                    this.flagCheckedListBox.EnumValue = enumValue;
                    windowsFormsEditorService.DropDownControl(this.flagCheckedListBox);
                    return this.flagCheckedListBox.EnumValue;
                }
            }
            return null;
        }

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.DropDown;
        }

        private FlagCheckedListBox flagCheckedListBox;
    }
}
