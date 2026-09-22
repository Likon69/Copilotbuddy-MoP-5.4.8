using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace Infralution.Localization.Wpf
{
    public partial class CultureSelectWindow : Window
    {
        public CultureSelectWindow()
        {
            InitializeComponent();
            List<CultureInfo> list = new List<CultureInfo>
            {
                new CultureInfo("de-DE"),
                new CultureInfo("fr-FR"),
                new CultureInfo("ru-RU"),
                new CultureInfo("tr-TR"),
                new CultureInfo("zh-Hans"),
                new CultureInfo("ko"),
                new CultureInfo("en-US"),
                new CultureInfo("es-ES"),
                new CultureInfo("it-IT")
            };
            list.Sort(new CultureInfoComparer());
            _cultureCombo.ItemsSource = list;
            _cultureCombo.SelectedItem = CultureManager.UICulture;
        }

        private void _cultureCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CultureManager.UICulture = _cultureCombo.SelectedItem as CultureInfo;
        }

        private class CultureInfoComparer : Comparer<CultureInfo>
        {
            public override int Compare(CultureInfo x, CultureInfo y)
            {
                return x.DisplayName.CompareTo(y.DisplayName);
            }
        }
    }
}
