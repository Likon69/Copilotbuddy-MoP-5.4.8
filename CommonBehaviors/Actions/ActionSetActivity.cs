using System;
using System.ComponentModel;
using Styx.CommonBot;
using Styx.TreeSharp;

namespace CommonBehaviors.Actions
{
    [Localizable(true)]
    public class ActionSetActivity : Styx.TreeSharp.Action
    {
        public delegate string GetStatusTextDelegate(object context);

        private readonly GetStatusTextDelegate _getStatusText;

        public ActionSetActivity([Localizable(false)] string statusText)
        {
            _getStatusText = context => statusText;
        }

        public ActionSetActivity([Localizable(true)] string format, params object[] args)
            : this(string.Format(format, args))
        {
        }

        public ActionSetActivity(GetStatusTextDelegate getStatusText)
        {
            _getStatusText = getStatusText;
        }

        protected override RunStatus Run(object context)
        {
            TreeRoot.StatusText = _getStatusText(context);

            if (Parent is Selector)
                return RunStatus.Failure;

            return RunStatus.Success;
        }
    }
}
