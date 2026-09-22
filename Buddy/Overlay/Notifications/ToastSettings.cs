using System;

namespace Buddy.Overlay.Notifications
{
    public class ToastSettings
    {
        public ToastSettings(bool displayTopToBottom = false)
        {
            this.DisplayTopToBottom = displayTopToBottom;
        }

        public bool DisplayTopToBottom { get; private set; }
    }
}
