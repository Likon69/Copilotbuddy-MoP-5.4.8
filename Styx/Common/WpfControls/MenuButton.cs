using System;

namespace Styx.Common.WpfControls
{
    public class MenuButton : SplitButton
    {
        public MenuButton()
        {
            base.DefaultStyleKey = typeof(MenuButton);
        }

        protected override void OnClick()
        {
            base.OpenButtonMenu();
        }
    }
}
