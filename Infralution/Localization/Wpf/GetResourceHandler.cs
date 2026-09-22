using System;
using System.Globalization;

namespace Infralution.Localization.Wpf
{
    public delegate object GetResourceHandler(string resxName, string key, CultureInfo culture);
}
