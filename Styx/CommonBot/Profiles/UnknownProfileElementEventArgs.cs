using System;
using System.Xml.Linq;

namespace Styx.CommonBot.Profiles
{
    public class UnknownProfileElementEventArgs : EventArgs
    {
        internal UnknownProfileElementEventArgs(XElement e)
        {
            Element = e;
        }

        public XElement Element { get; set; }

        public bool Handled { get; set; }
    }
}
