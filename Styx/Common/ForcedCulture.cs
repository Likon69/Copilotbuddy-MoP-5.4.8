using System;
using System.Globalization;
using System.Threading;

namespace Styx.Common
{
    public class ForcedCulture : IDisposable
    {
        private CultureInfo _defaultCulture;

        public ForcedCulture()
        {
            _defaultCulture = Thread.CurrentThread.CurrentCulture;
            var newCulture = new CultureInfo(_defaultCulture.Name) { NumberFormat = { CurrencyDecimalSeparator = ".", NumberDecimalSeparator = ".", NaNSymbol = "NaN" } };

            Thread.CurrentThread.CurrentCulture = newCulture;
        }

        public void Dispose()
        {
            Thread.CurrentThread.CurrentCulture = _defaultCulture;
        }
    }
}
