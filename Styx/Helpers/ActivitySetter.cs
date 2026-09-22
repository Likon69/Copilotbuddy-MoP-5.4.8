using System;
using Styx.CommonBot;

namespace Styx.Helpers
{
    public class ActivitySetter : IDisposable
    {
        private readonly string _oldText;

        public ActivitySetter(string text)
        {
            _oldText = TreeRoot.StatusText;
            TreeRoot.StatusText = text;
        }

        public void Dispose()
        {
            if (!string.IsNullOrEmpty(_oldText))
                TreeRoot.StatusText = _oldText;
        }
    }
}
