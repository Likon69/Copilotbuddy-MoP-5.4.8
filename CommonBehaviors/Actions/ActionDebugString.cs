using Styx.TreeSharp;

namespace CommonBehaviors.Actions
{
    public class ActionDebugString : Styx.TreeSharp.Action
    {
        private string _message;
        private readonly DebugStringDelegate _stringDelegate;

        public ActionDebugString(DebugStringDelegate stringDelegate)
        {
            _stringDelegate = stringDelegate;
        }

        public ActionDebugString(string message)
        {
            _message = message;
        }

        protected override RunStatus Run(object context)
        {
            if (_stringDelegate != null)
                _message = _stringDelegate(context);

            if (Parent != null && Parent is Selector)
                return RunStatus.Failure;

            return RunStatus.Success;
        }
    }
}
