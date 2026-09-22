using System.Windows.Markup;

namespace Infralution.Localization.Wpf
{
    [MarkupExtensionReturnType(typeof(XmlLanguage))]
    public class UICultureExtension : ManagedMarkupExtension
    {
        private static MarkupExtensionManager _markupManager = new MarkupExtensionManager(2);

        public UICultureExtension()
            : base(_markupManager)
        {
        }

        protected override object GetValue()
        {
            return XmlLanguage.GetLanguage(CultureManager.UICulture.IetfLanguageTag);
        }

        public static MarkupExtensionManager MarkupManager
        {
            get { return _markupManager; }
        }

        public static void UpdateAllTargets()
        {
            _markupManager.UpdateAllTargets();
        }
    }
}
