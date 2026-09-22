using System.Collections.Generic;

namespace Infralution.Localization.Wpf
{
    public class MarkupExtensionManager
    {
        private List<ManagedMarkupExtension> _activeExtensions = new List<ManagedMarkupExtension>();
        private int _cleanupCount;
        private int _cleanupInterval = 40;

        public MarkupExtensionManager(int cleanupInterval)
        {
            _cleanupInterval = cleanupInterval;
        }

        public virtual void UpdateAllTargets()
        {
            foreach (ManagedMarkupExtension managedMarkupExtension in _activeExtensions)
            {
                managedMarkupExtension.UpdateTargets();
            }
        }

        public List<ManagedMarkupExtension> ActiveExtensions
        {
            get { return _activeExtensions; }
        }

        public void CleanupInactiveExtensions()
        {
            List<ManagedMarkupExtension> list = new List<ManagedMarkupExtension>(_activeExtensions.Count);
            foreach (ManagedMarkupExtension managedMarkupExtension in _activeExtensions)
            {
                if (managedMarkupExtension.IsTargetAlive)
                {
                    list.Add(managedMarkupExtension);
                }
            }
            _activeExtensions = list;
        }

        internal void RegisterExtension(ManagedMarkupExtension extension)
        {
            if (_cleanupCount > _cleanupInterval)
            {
                CleanupInactiveExtensions();
                _cleanupCount = 0;
            }
            _activeExtensions.Add(extension);
            _cleanupCount++;
        }
    }
}
