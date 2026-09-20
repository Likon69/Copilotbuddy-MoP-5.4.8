using System;
using System.Xml;
using System.Xml.Linq;
using Styx.Common;

namespace Styx.Helpers
{
    public static class XmlExtensions
    {
        public static void Error(this XObject actualElement, string errorMessage)
        {
            Logging.WriteDiagnostic(actualElement.GetError(errorMessage));
        }

        public static string GetError(this XObject actualElement, string errorMessage)
        {
            if (((IXmlLineInfo)actualElement).HasLineInfo())
                return string.Format("XML Error: {0} - On line {1}", errorMessage, ((IXmlLineInfo)actualElement).LineNumber);

            return string.Format("XML Error: {0} - [{1}]", errorMessage, actualElement);
        }
    }
}
