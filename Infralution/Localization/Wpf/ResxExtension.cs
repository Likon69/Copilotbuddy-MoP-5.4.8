using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media.Imaging;

namespace Infralution.Localization.Wpf
{
    [ContentProperty("Children")]
    [MarkupExtensionReturnType(typeof(object))]
    public class ResxExtension : ManagedMarkupExtension
    {
        private string _resxName;
        private string _defaultResxName;
        private string _key;
        private string _defaultValue;
        private ResourceManager _resourceManager;
        private Binding _binding;
        private Collection<ResxExtension> _children = new Collection<ResxExtension>();
        private static Dictionary<string, WeakReference> _resourceManagers = new Dictionary<string, WeakReference>();
        private static MarkupExtensionManager _markupManager = new MarkupExtensionManager(40);

        public static readonly DependencyProperty DefaultResxNameProperty = DependencyProperty.RegisterAttached(
            "DefaultResxName",
            typeof(string),
            typeof(ResxExtension),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits,
                new PropertyChangedCallback(OnDefaultResxNamePropertyChanged)));

        public static event GetResourceHandler GetResource;

        public ResxExtension()
            : base(_markupManager)
        {
        }

        public ResxExtension(string key)
            : base(_markupManager)
        {
            _key = key;
        }

        public string ResxName
        {
            get
            {
                string text = _resxName;
                if (string.IsNullOrEmpty(text))
                {
                    if (_defaultResxName == null)
                    {
                        WeakReference weakReference = base.TargetObjects.Find((WeakReference target) => target.IsAlive);
                        if (weakReference != null && weakReference.Target is DependencyObject)
                        {
                            _defaultResxName = (weakReference.Target as DependencyObject).GetValue(DefaultResxNameProperty) as string;
                        }
                    }
                    text = _defaultResxName;
                }
                return text;
            }
            set { _resxName = value; }
        }

        public string Key
        {
            get { return _key; }
            set { _key = value; }
        }

        public string DefaultValue
        {
            get { return _defaultValue; }
            set { _defaultValue = value; }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public Collection<ResxExtension> Children
        {
            get { return _children; }
        }

        public Binding Binding
        {
            get
            {
                if (_binding == null)
                {
                    _binding = new Binding();
                }
                return _binding;
            }
        }

        [DefaultValue(null)]
        public string BindingElementName
        {
            get { return Binding.ElementName; }
            set { Binding.ElementName = value; }
        }

        [DefaultValue(null)]
        public PropertyPath BindingPath
        {
            get { return Binding.Path; }
            set { Binding.Path = value; }
        }

        [DefaultValue(null)]
        public RelativeSource BindingRelativeSource
        {
            get { return Binding.RelativeSource; }
            set { Binding.RelativeSource = value; }
        }

        [DefaultValue(null)]
        public object BindingSource
        {
            get { return Binding.Source; }
            set { Binding.Source = value; }
        }

        [DefaultValue(null)]
        public string BindingXPath
        {
            get { return Binding.XPath; }
            set { Binding.XPath = value; }
        }

        [DefaultValue(null)]
        public IValueConverter BindingConverter
        {
            get { return Binding.Converter; }
            set { Binding.Converter = value; }
        }

        [DefaultValue(null)]
        public string BindingGroupName
        {
            get { return Binding.BindingGroupName; }
            set { Binding.BindingGroupName = value; }
        }

        [DefaultValue(null)]
        public CultureInfo BindingConverterCulture
        {
            get { return Binding.ConverterCulture; }
            set { Binding.ConverterCulture = value; }
        }

        [DefaultValue(null)]
        public object BindingConverterParameter
        {
            get { return Binding.ConverterParameter; }
            set { Binding.ConverterParameter = value; }
        }

        [DefaultValue(false)]
        public bool BindsDirectlyToSource
        {
            get { return Binding.BindsDirectlyToSource; }
            set { Binding.BindsDirectlyToSource = value; }
        }

        [DefaultValue(BindingMode.Default)]
        public BindingMode BindingMode
        {
            get { return Binding.Mode; }
            set { Binding.Mode = value; }
        }

        [DefaultValue(false)]
        public bool BindingNotifyOnSourceUpdated
        {
            get { return Binding.NotifyOnSourceUpdated; }
            set { Binding.NotifyOnSourceUpdated = value; }
        }

        [DefaultValue(false)]
        public bool BindingNotifyOnTargetUpdated
        {
            get { return Binding.NotifyOnTargetUpdated; }
            set { Binding.NotifyOnTargetUpdated = value; }
        }

        [DefaultValue(false)]
        public bool BindingNotifyOnValidationError
        {
            get { return Binding.NotifyOnValidationError; }
            set { Binding.NotifyOnValidationError = value; }
        }

        [DefaultValue(null)]
        public object BindingAsyncState
        {
            get { return Binding.AsyncState; }
            set { Binding.AsyncState = value; }
        }

        [DefaultValue(false)]
        public bool BindingIsAsync
        {
            get { return Binding.IsAsync; }
            set { Binding.IsAsync = value; }
        }

        [DefaultValue(null)]
        public object BindingFallbackValue
        {
            get { return Binding.FallbackValue; }
            set { Binding.FallbackValue = value; }
        }

        [DefaultValue(null)]
        public object BindingTargetNullValue
        {
            get { return Binding.TargetNullValue; }
            set { Binding.TargetNullValue = value; }
        }

        [DefaultValue(false)]
        public bool BindingValidatesOnDataErrors
        {
            get { return Binding.ValidatesOnDataErrors; }
            set { Binding.ValidatesOnDataErrors = value; }
        }

        [DefaultValue(false)]
        public bool BindingValidatesOnExceptions
        {
            get { return Binding.ValidatesOnExceptions; }
            set { Binding.ValidatesOnExceptions = value; }
        }

        [DefaultValue(UpdateSourceTrigger.Default)]
        public UpdateSourceTrigger BindingUpdateSourceTrigger
        {
            get { return Binding.UpdateSourceTrigger; }
            set { Binding.UpdateSourceTrigger = value; }
        }

        [DefaultValue(false)]
        public Collection<ValidationRule> BindingValidationRules
        {
            get { return Binding.ValidationRules; }
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            RegisterTarget(serviceProvider);
            if (base.IsInDesignMode)
            {
                CultureManager.EnsureCultureNotifyIcon();
            }
            if (string.IsNullOrEmpty(Key) && !IsBindingExpression)
            {
                throw new ArgumentException("You must set the resource Key or Binding properties");
            }
            object obj;
            if (base.TargetProperty != null && !IsMultiBindingChild)
            {
                if (IsMultiBindingParent)
                {
                    MultiBinding multiBinding = CreateMultiBinding();
                    obj = multiBinding.ProvideValue(serviceProvider);
                }
                else if (IsBindingExpression)
                {
                    Binding binding = CreateBinding();
                    obj = binding.ProvideValue(serviceProvider);
                }
                else
                {
                    obj = GetValue();
                }
            }
            else
            {
                obj = this;
            }
            return obj;
        }

        public static MarkupExtensionManager MarkupManager
        {
            get { return _markupManager; }
        }

        public static void UpdateAllTargets()
        {
            _markupManager.UpdateAllTargets();
        }

        public static void UpdateTarget(string key)
        {
            foreach (ManagedMarkupExtension managedMarkupExtension in _markupManager.ActiveExtensions)
            {
                ResxExtension resxExtension = (ResxExtension)managedMarkupExtension;
                if (resxExtension.Key == key)
                {
                    resxExtension.UpdateTargets();
                }
            }
        }

        [AttachedPropertyBrowsableForChildren(IncludeDescendants = true)]
        public static string GetDefaultResxName(DependencyObject target)
        {
            return (string)target.GetValue(DefaultResxNameProperty);
        }

        public static void SetDefaultResxName(DependencyObject target, string value)
        {
            target.SetValue(DefaultResxNameProperty, value);
        }

        private Binding CreateBinding()
        {
            Binding binding = new Binding();
            if (IsBindingExpression)
            {
                if (_binding.ElementName != null)
                {
                    binding.ElementName = _binding.ElementName;
                }
                if (_binding.RelativeSource != null)
                {
                    binding.RelativeSource = _binding.RelativeSource;
                }
                if (_binding.Source != null)
                {
                    binding.Source = _binding.Source;
                }
                binding.AsyncState = _binding.AsyncState;
                binding.BindingGroupName = _binding.BindingGroupName;
                binding.BindsDirectlyToSource = _binding.BindsDirectlyToSource;
                binding.Converter = _binding.Converter;
                binding.ConverterCulture = _binding.ConverterCulture;
                binding.ConverterParameter = _binding.ConverterParameter;
                binding.FallbackValue = _binding.FallbackValue;
                binding.IsAsync = _binding.IsAsync;
                binding.Mode = _binding.Mode;
                binding.NotifyOnSourceUpdated = _binding.NotifyOnSourceUpdated;
                binding.NotifyOnTargetUpdated = _binding.NotifyOnTargetUpdated;
                binding.NotifyOnValidationError = _binding.NotifyOnValidationError;
                binding.Path = _binding.Path;
                binding.TargetNullValue = _binding.TargetNullValue;
                binding.UpdateSourceTrigger = _binding.UpdateSourceTrigger;
                binding.ValidatesOnDataErrors = _binding.ValidatesOnDataErrors;
                binding.ValidatesOnExceptions = _binding.ValidatesOnExceptions;
                foreach (ValidationRule validationRule in _binding.ValidationRules)
                {
                    binding.ValidationRules.Add(validationRule);
                }
                binding.XPath = _binding.XPath;
                binding.StringFormat = GetValue() as string;
            }
            else
            {
                binding.Source = GetValue();
            }
            return binding;
        }

        private MultiBinding CreateMultiBinding()
        {
            MultiBinding multiBinding = new MultiBinding();
            foreach (ResxExtension resxExtension in _children)
            {
                if (resxExtension.ResxName == null)
                {
                    resxExtension.ResxName = ResxName;
                }
                multiBinding.Bindings.Add(resxExtension.CreateBinding());
            }
            multiBinding.StringFormat = GetValue() as string;
            return multiBinding;
        }

        private bool IsBindingExpression
        {
            get
            {
                return _binding != null && (_binding.Source != null || _binding.RelativeSource != null ||
                    _binding.ElementName != null || _binding.XPath != null || _binding.Path != null);
            }
        }

        private bool IsMultiBindingParent
        {
            get { return _children.Count > 0; }
        }

        private bool IsMultiBindingChild
        {
            get { return base.TargetPropertyType == typeof(Collection<ResxExtension>); }
        }

        protected override object GetValue()
        {
            if (string.IsNullOrEmpty(Key))
            {
                return null;
            }
            object obj = null;
            if (!string.IsNullOrEmpty(ResxName))
            {
                try
                {
                    if (GetResource != null)
                    {
                        obj = GetResource(ResxName, Key, CultureManager.UICulture);
                    }
                    if (obj == null)
                    {
                        if (_resourceManager == null)
                        {
                            _resourceManager = GetResourceManager(ResxName);
                        }
                        if (_resourceManager != null)
                        {
                            obj = _resourceManager.GetObject(Key, CultureManager.UICulture);
                        }
                    }
                    if (!IsMultiBindingChild)
                    {
                        obj = ConvertValue(obj);
                    }
                }
                catch
                {
                }
            }
            if (obj == null)
            {
                obj = GetDefaultValue(Key);
            }
            return obj;
        }

        protected override void UpdateTarget(object target)
        {
            if (IsMultiBindingChild)
            {
                return;
            }
            if (IsMultiBindingParent)
            {
                FrameworkElement frameworkElement = target as FrameworkElement;
                if (frameworkElement != null)
                {
                    MultiBinding multiBinding = CreateMultiBinding();
                    frameworkElement.SetBinding(base.TargetProperty as DependencyProperty, multiBinding);
                    return;
                }
            }
            else if (IsBindingExpression)
            {
                FrameworkElement frameworkElement2 = target as FrameworkElement;
                if (frameworkElement2 != null)
                {
                    Binding binding = CreateBinding();
                    frameworkElement2.SetBinding(base.TargetProperty as DependencyProperty, binding);
                    return;
                }
            }
            else
            {
                base.UpdateTarget(target);
            }
        }

        private bool HasEmbeddedResx(Assembly assembly, string resxName)
        {
            if (assembly.GetType().Name == "AssemblyBuilder")
            {
                return false;
            }
            try
            {
                string[] manifestResourceNames = assembly.GetManifestResourceNames();
                string text = resxName.ToLower() + ".resources";
                foreach (string text2 in manifestResourceNames)
                {
                    if (text2.ToLower() == text)
                    {
                        return true;
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private Assembly FindResourceAssembly()
        {
            Assembly entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly != null && HasEmbeddedResx(entryAssembly, ResxName))
            {
                return entryAssembly;
            }
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                string fullName = assembly.FullName;
                if (!fullName.StartsWith("Microsoft.") && !fullName.StartsWith("System.") &&
                    !fullName.StartsWith("System,") && !fullName.StartsWith("mscorlib,") &&
                    !fullName.StartsWith("PresentationFramework,") && !fullName.StartsWith("WindowsBase,") &&
                    HasEmbeddedResx(assembly, ResxName))
                {
                    return assembly;
                }
            }
            return null;
        }

        private ResourceManager GetResourceManager(string resxName)
        {
            WeakReference weakReference = null;
            ResourceManager resourceManager = null;
            if (resxName == null)
            {
                return null;
            }
            if (_resourceManagers.TryGetValue(resxName, out weakReference))
            {
                resourceManager = weakReference.Target as ResourceManager;
                if (resourceManager == null)
                {
                    _resourceManagers.Remove(resxName);
                }
            }
            if (resourceManager == null)
            {
                Assembly assembly = FindResourceAssembly();
                if (assembly != null)
                {
                    resourceManager = new ResourceManager(resxName, assembly);
                }
                _resourceManagers.Add(resxName, new WeakReference(resourceManager));
            }
            return resourceManager;
        }

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        private object ConvertValue(object value)
        {
            BitmapSource bitmapSource = null;
            if (value is Icon)
            {
                Icon icon = value as Icon;
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    icon.Save(memoryStream);
                    memoryStream.Seek(0L, SeekOrigin.Begin);
                    bitmapSource = BitmapFrame.Create(memoryStream);
                }
            }
            else if (value is Bitmap)
            {
                Bitmap bitmap = value as Bitmap;
                IntPtr hbitmap = bitmap.GetHbitmap();
                bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                    hbitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
                bitmapSource.Freeze();
                DeleteObject(hbitmap);
            }
            object obj;
            if (bitmapSource != null)
            {
                if (base.TargetPropertyType == typeof(object))
                {
                    obj = new global::System.Windows.Controls.Image
                    {
                        Source = bitmapSource,
                        Width = bitmapSource.Width,
                        Height = bitmapSource.Height
                    };
                }
                else
                {
                    obj = bitmapSource;
                }
            }
            else
            {
                obj = value;
                Type targetPropertyType = base.TargetPropertyType;
                if (targetPropertyType != null && value is string && targetPropertyType != typeof(string) &&
                    targetPropertyType != typeof(object))
                {
                    TypeConverter converter = TypeDescriptor.GetConverter(targetPropertyType);
                    obj = converter.ConvertFromInvariantString(value as string);
                }
            }
            return obj;
        }

        private object GetDefaultValue(string key)
        {
            object obj = _defaultValue;
            Type targetPropertyType = base.TargetPropertyType;
            if (_defaultValue == null)
            {
                if (targetPropertyType == typeof(string) || targetPropertyType == typeof(object) || IsMultiBindingChild)
                {
                    obj = "#" + key;
                }
            }
            else if (targetPropertyType != null && targetPropertyType != typeof(string) && targetPropertyType != typeof(object))
            {
                try
                {
                    TypeConverter converter = TypeDescriptor.GetConverter(targetPropertyType);
                    obj = converter.ConvertFromInvariantString(_defaultValue);
                }
                catch
                {
                }
            }
            return obj;
        }

        private static void OnDefaultResxNamePropertyChanged(DependencyObject element, DependencyPropertyChangedEventArgs args)
        {
            if (DesignerProperties.GetIsInDesignMode(element))
            {
                foreach (ManagedMarkupExtension managedMarkupExtension in MarkupManager.ActiveExtensions)
                {
                    ResxExtension resxExtension = (ResxExtension)managedMarkupExtension;
                    if (resxExtension.IsTarget(element))
                    {
                        resxExtension._resourceManager = null;
                        resxExtension._defaultResxName = args.NewValue as string;
                        resxExtension.UpdateTarget(element);
                    }
                }
            }
        }
    }
}
