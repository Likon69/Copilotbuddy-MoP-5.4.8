using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Markup;

namespace Infralution.Localization.Wpf
{
    public abstract class ManagedMarkupExtension : MarkupExtension
    {
        private List<WeakReference> list_0 = new List<WeakReference>();
        private object object_0;

        public ManagedMarkupExtension(MarkupExtensionManager manager)
        {
            manager.method_0(this);
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            RegisterTarget(serviceProvider);
            object obj = this;
            if (object_0 != null)
            {
                obj = GetValue();
            }
            return obj;
        }

        protected virtual void RegisterTarget(IServiceProvider serviceProvider)
        {
            IProvideValueTarget provideValueTarget = serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
            object targetObject = provideValueTarget.TargetObject;
            if (targetObject != null && targetObject.GetType().FullName != "System.Windows.SharedDp")
            {
                object_0 = provideValueTarget.TargetProperty;
                list_0.Add(new WeakReference(targetObject));
            }
        }

        protected virtual void UpdateTarget(object target)
        {
            if (object_0 is DependencyProperty)
            {
                DependencyObject dependencyObject = target as DependencyObject;
                if (dependencyObject != null)
                {
                    dependencyObject.SetValue(object_0 as DependencyProperty, GetValue());
                    return;
                }
            }
            else if (object_0 is PropertyInfo)
            {
                (object_0 as PropertyInfo).SetValue(target, GetValue(), null);
            }
        }

        public void UpdateTargets()
        {
            foreach (WeakReference weakReference in list_0)
            {
                if (weakReference.IsAlive)
                {
                    UpdateTarget(weakReference.Target);
                }
            }
        }

        public bool IsTarget(object target)
        {
            foreach (WeakReference weakReference in list_0)
            {
                if (weakReference.IsAlive && weakReference.Target == target)
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsTargetAlive
        {
            get
            {
                if (list_0.Count == 0)
                {
                    return true;
                }
                foreach (WeakReference weakReference in list_0)
                {
                    if (weakReference.IsAlive)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public bool IsInDesignMode
        {
            get
            {
                foreach (WeakReference weakReference in list_0)
                {
                    DependencyObject dependencyObject = weakReference.Target as DependencyObject;
                    if (dependencyObject != null && DesignerProperties.GetIsInDesignMode(dependencyObject))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        protected List<WeakReference> TargetObjects
        {
            get { return list_0; }
        }

        protected object TargetProperty
        {
            get { return object_0; }
        }

        protected Type TargetPropertyType
        {
            get
            {
                Type type = null;
                if (object_0 is DependencyProperty)
                {
                    type = (object_0 as DependencyProperty).PropertyType;
                }
                else if (object_0 is PropertyInfo)
                {
                    type = (object_0 as PropertyInfo).PropertyType;
                }
                else if (object_0 != null)
                {
                    type = object_0.GetType();
                }
                return type;
            }
        }

        protected abstract object GetValue();
    }
}
