using System;
using System.Xml.Linq;
using Styx.Helpers;
using Styx.Resources;

namespace Styx.CommonBot.Profiles
{
    public class ProfileAttributeExpectedException<T> : ProfileException
    {
        public ProfileAttributeExpectedException(XAttribute attribute, params string[] validValues)
            : base(attribute.GetError(string.Format(StyxResources.Value___0___in___1___attribute_is_not_supported__Expected__2__type__3_, attribute.Value, attribute.Name, ProfileExceptionHelper.GetTypeString<T>(),
            validValues != null && validValues.Length > 0 ? Environment.NewLine + StyxResources.Valid_values_are__ + ProfileExceptionHelper.GetValidTagsString(validValues) : "")))
        {
        }
    }

    public class ProfileAttributeExpectedException : ProfileException
    {
        public ProfileAttributeExpectedException(XAttribute attribute, params string[] validValues)
            : base(attribute.GetError(string.Format(StyxResources.Value___0___in___1___is_not_supported__2_, attribute.Value, attribute.Name,
            validValues == null || validValues.Length <= 0 ? "" : StyxResources._Valid_values_are__ + ProfileExceptionHelper.GetValidTagsString(validValues))))
        {
        }
    }
}
