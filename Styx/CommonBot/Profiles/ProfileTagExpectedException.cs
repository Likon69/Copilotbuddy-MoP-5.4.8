using System;
using System.Xml.Linq;
using Styx.Helpers;
using Styx.Resources;

namespace Styx.CommonBot.Profiles
{
    public class ProfileTagExpectedException : ProfileException
    {
        public ProfileTagExpectedException(string message)
            : base(message)
        {
        }

        public static ProfileTagExpectedException Create<T>(XElement tag, params string[] validValues)
        {
            return new ProfileTagExpectedException(tag.GetError(string.Format(StyxResources.Value___0___in___1___tag_is_not_supported__Expected__2__type__3_,
                tag.Value,
                tag.Name,
                ProfileExceptionHelper.GetTypeString<T>(),
                validValues == null || validValues.Length <= 0 ? "" : Environment.NewLine + StyxResources.Valid_values_are__ + ProfileExceptionHelper.GetValidTagsString(validValues))));
        }
    }
}
