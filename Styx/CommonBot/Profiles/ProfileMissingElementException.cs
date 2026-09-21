using System;
using System.Xml.Linq;
using Styx.Helpers;
using Styx.Resources;

namespace Styx.CommonBot.Profiles
{
    public class ProfileMissingElementException : ProfileException
    {
        public ProfileMissingElementException(string elementName, XElement parent)
            : base(parent.GetError(string.Format(StyxResources.Missing_subelement__0__in___1___, elementName, parent.Name)))
        {
        }
    }
}
