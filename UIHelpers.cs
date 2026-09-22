using System.ComponentModel;
using Styx.CommonBot;

namespace CopilotBuddy
{
    internal class UIHelpers : INotifyPropertyChanged
    {
        private static UIHelpers _instance;
        private string _statusText;

        public UIHelpers()
        {
            TreeRoot.OnStatusTextChanged += delegate(object sender, StatusTextChangedEventArgs e)
            {
                Instance.StatusText = e.NewStatus;
            };
        }

        public static UIHelpers Instance
        {
            get { return _instance ?? (_instance = new UIHelpers()); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void Changed(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }

        public string StatusText
        {
            get { return _statusText; }
            set
            {
                if (_statusText != value)
                {
                    _statusText = value;
                    Changed("StatusText");
                }
            }
        }
    }
}
