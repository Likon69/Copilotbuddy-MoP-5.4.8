using System.Collections.Generic;

namespace Infralution.Localization.Wpf
{
    public class MarkupExtensionManager
    {
        private List<ManagedMarkupExtension> list_0 = new List<ManagedMarkupExtension>();
        private int int_0;
        private int int_1 = 40;

        public MarkupExtensionManager(int cleanupInterval)
        {
            int_1 = cleanupInterval;
        }

        public virtual void UpdateAllTargets()
        {
            foreach (ManagedMarkupExtension managedMarkupExtension in list_0)
            {
                managedMarkupExtension.UpdateTargets();
            }
        }

        public List<ManagedMarkupExtension> ActiveExtensions
        {
            get { return list_0; }
        }

        public void CleanupInactiveExtensions()
        {
            List<ManagedMarkupExtension> list = new List<ManagedMarkupExtension>(list_0.Count);
            foreach (ManagedMarkupExtension managedMarkupExtension in list_0)
            {
                if (managedMarkupExtension.IsTargetAlive)
                {
                    list.Add(managedMarkupExtension);
                }
            }
            list_0 = list;
        }

        internal void method_0(ManagedMarkupExtension managedMarkupExtension_0)
        {
            if (int_0 > int_1)
            {
                CleanupInactiveExtensions();
                int_0 = 0;
            }
            list_0.Add(managedMarkupExtension_0);
            int_0++;
        }
    }
}
