using System.Collections.Generic;
using System.Linq;
using Styx.TreeSharp;
using Action = Styx.TreeSharp.Action;

namespace Styx.Common
{
    public class HookExecutor : Action
    {
        public HookExecutor(string location, string description = null, Composite defaultComposite = null)
        {
            _location = location;
            _defaultComposite = defaultComposite;

            if (description != null && !TreeHooks.Instance.HookDescriptions.Any(hookDescription => hookDescription.Name == location && hookDescription.Description == description))
            {
                TreeHooks.Instance.HookDescriptions.Add(new HookDescription(location, description));
            }
        }

        public override void Start(object context)
        {
            base.Start(context);
            int revision = TreeHooks.Instance.GetHookRevision(_location);
            if (revision != _hookRevision)
            {
                _hookRevision = revision;
                var composites = new List<Composite>();
                if (_defaultComposite != null)
                    composites.Add(_defaultComposite);
                TreeHooks.Instance.ApplyCompositeOperations(_location, composites);
                var prioritySelector = new PrioritySelector();
                composites.ForEach(prioritySelector.AddChild);
                _composite = prioritySelector;
            }
            _composite.Start(context);
        }

        public override void Stop(object context)
        {
            if (_composite != null)
                _composite.Stop(context);
            base.Stop(context);
        }

        protected override RunStatus Run(object context)
        {
            return _composite.Tick(context);
        }

        private readonly Composite _defaultComposite;
        private readonly string _location;
        private Composite _composite;
        private int _hookRevision = -1;
    }
}
