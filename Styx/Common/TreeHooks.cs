using System;
using System.Collections.Generic;
using Styx.TreeSharp;

namespace Styx.Common
{
    public sealed class TreeHooks
    {
        private TreeHooks()
        {
            Hooks = new Dictionary<string, List<CompositeListOperation>>();
            HookDescriptions = new List<HookDescription>();
            _hookRevisions = new Dictionary<string, int>();
        }

        public static TreeHooks Instance
        {
            get
            {
                if (ThisInstance == null)
                    ThisInstance = new TreeHooks();
                return ThisInstance;
            }
        }

        public List<HookDescription> HookDescriptions { get; private set; }
        public Dictionary<string, List<CompositeListOperation>> Hooks { get; private set; }

        public event System.Action HooksCleared;

        public void AddHook(string location, Composite behavior)
        {
            RegisterHookChange(location);
            Hooks[location].Add(new AddCompositeListOperation(behavior));
            Logging.WriteDiagnostic("Added new hook [" + location + "] " + behavior.Guid);
        }

        public void InsertHook(string location, int index, Composite behavior)
        {
            RegisterHookChange(location);
            Hooks[location].Add(new InsertCompositeListOperation(behavior, index));
            Logging.WriteDiagnostic("Inserted new hook [" + location + " @" + index + "] " + behavior.Guid);
        }

        private void RegisterHookChange(string location)
        {
            if (!Hooks.ContainsKey(location))
                Hooks.Add(location, new List<CompositeListOperation>());

            int revision;
            if (!_hookRevisions.TryGetValue(location, out revision))
            {
                _hookRevisions[location] = 1;
                return;
            }
            _hookRevisions[location] = revision + 1;
        }

        public void RemoveHook(string location, Composite behavior)
        {
            List<CompositeListOperation> operations;
            if (!Hooks.TryGetValue(location, out operations))
                return;

            if (operations.RemoveAll(operation => operation.Composite == behavior) > 0)
                RegisterHookChange(location);

            Logging.WriteDiagnostic("Removed hook [" + location + "] " + behavior.Guid);
        }

        public void ReplaceHook(string location, Composite behavior)
        {
            RegisterHookChange(location);
            Hooks[location].Clear();
            Hooks[location].Add(new ReplaceCompositeListOperation(behavior));
            if (behavior != null)
                Logging.WriteDiagnostic("Replaced hook [" + location + "] " + behavior.Guid);
        }

        public void ClearAll()
        {
            Hooks.Clear();
            HookDescriptions.Clear();
            if (HooksCleared != null)
                HooksCleared();
        }

        public void ApplyCompositeOperations(string location, List<Composite> compositeList)
        {
            if (compositeList == null)
                throw new ArgumentNullException("compositeList");

            List<CompositeListOperation> operations;
            if (!Hooks.TryGetValue(location, out operations))
                return;

            foreach (CompositeListOperation operation in operations)
                operation.ApplyTo(compositeList);
        }

        internal int GetHookRevision(string location)
        {
            int revision;
            if (!_hookRevisions.TryGetValue(location, out revision))
                return 0;
            return revision;
        }

        private static TreeHooks ThisInstance;
        private readonly Dictionary<string, int> _hookRevisions;
    }
}
