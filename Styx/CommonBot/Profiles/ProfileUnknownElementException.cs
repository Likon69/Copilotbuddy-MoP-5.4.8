using System;
using System.Xml.Linq;
using Styx.Helpers;
using Styx.Resources;

namespace Styx.CommonBot.Profiles
{
    public class ProfileUnknownElementException : ProfileException
    {
        public ProfileUnknownElementException(XElement element)
            : this(element, null)
        {
        }

        public ProfileUnknownElementException(XElement element, params string[] validTags)
            : base(element.GetError(string.Format(StyxResources.Unknown_tag___0____Input____1____in___2____3_, element.Name, element, element.Parent != null && element.Parent.Name != null ? element.Parent.Name : StyxResources.Unknown,
            validTags != null && validTags.Length > 0 ? Environment.NewLine + StyxResources.Valid_tags_are__ + ProfileExceptionHelper.GetValidTagsString(validTags) : "")))
        {
        }
    }
}
