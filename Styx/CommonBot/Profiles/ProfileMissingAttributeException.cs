using System;
using System.Xml.Linq;
using Styx.Helpers;
using Styx.Resources;

namespace Styx.CommonBot.Profiles
{
    public class ProfileMissingAttributeException<T> : ProfileException
    {
        public ProfileMissingAttributeException(string attributeName, XElement tag)
            : base(tag.GetError(string.Format(StyxResources.Attribute__0__of__1__type_was_missing_in___2___tag_, attributeName, ProfileExceptionHelper.GetTypeString<T>(), tag.Name)))
        {
        }
    }

    public class ProfileMissingAttributeException : ProfileException
    {
        public ProfileMissingAttributeException(string attributeName, XElement tag, params string[] validValues)
            : base(tag.GetError(string.Format(StyxResources.Missing_attribute__0__in___1____2_, attributeName, tag.Name,
            validValues == null || validValues.Length <= 0 ? "" : StyxResources._Valid_values_are__ + ProfileExceptionHelper.GetValidTagsString(validValues))))
        {
        }
    }
}
