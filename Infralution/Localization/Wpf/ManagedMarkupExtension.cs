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
        private List<WeakReference> _targetObjects = new List<WeakReference>();
        private object _targetProperty;

        public ManagedMarkupExtension(MarkupExtensionManager manager)
        {
            manager.RegisterExtension(this);
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            RegisterTarget(serviceProvider);
            object obj = this;
            if (_targetProperty != null)
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
                _targetProperty = provideValueTarget.TargetProperty;
                _targetObjects.Add(new WeakReference(targetObject));
            }
        }

        protected virtual void UpdateTarget(object target)
        {
            if (_targetProperty is DependencyProperty)
            {
                DependencyObject dependencyObject = target as DependencyObject;
                if (dependencyObject != null)
                {
                    dependencyObject.SetValue(_targetProperty as DependencyProperty, GetValue());
                    return;
                }
            }
            else if (_targetProperty is PropertyInfo)
            {
                (_targetProperty as PropertyInfo).SetValue(target, GetValue(), null);
            }
        }

        public void UpdateTargets()
        {
            foreach (WeakReference weakReference in _targetObjects)
            {
                if (weakReference.IsAlive)
                {
                    UpdateTarget(weakReference.Target);
                }
            }
        }

        public bool IsTarget(object target)
        {
            foreach (WeakReference weakReference in _targetObjects)
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
                if (_targetObjects.Count == 0)
                {
                    return true;
                }
                foreach (WeakReference weakReference in _targetObjects)
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
                foreach (WeakReference weakReference in _targetObjects)
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
            get { return _targetObjects; }
        }

        protected object TargetProperty
        {
            get { return _targetProperty; }
        }

        protected Type TargetPropertyType
        {
            get
            {
                Type type = null;
                if (_targetProperty is DependencyProperty)
                {
                    type = (_targetProperty as DependencyProperty).PropertyType;
                }
                else if (_targetProperty is PropertyInfo)
                {
                    type = (_targetProperty as PropertyInfo).PropertyType;
                }
                else if (_targetProperty != null)
                {
                    type = _targetProperty.GetType();
                }
                return type;
            }
        }

        protected abstract object GetValue();
    }
}
