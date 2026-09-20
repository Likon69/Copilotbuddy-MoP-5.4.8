using System;
using System.Xml.Linq;
using Styx.Helpers;
using Styx.Resources;

namespace Styx.CommonBot.Profiles
{
    public class ProfileUnknownAttributeException : ProfileException
    {
        public ProfileUnknownAttributeException(XAttribute attribute)
            : this(attribute, null)
        {
        }

        public ProfileUnknownAttributeException(XAttribute attribute, params string[] validAttributes)
            : base(attribute.GetError(string.Format(StyxResources.Unknown_attribute___0____Input____1____in___2____3_, attribute.Name, attribute, attribute.Parent != null && attribute.Parent.Name != null ? attribute.Parent.Name : StyxResources.Unknown,
            validAttributes != null && validAttributes.Length > 0 ? Environment.NewLine + StyxResources.Valid_attributes_are__ + ProfileExceptionHelper.GetValidTagsString(validAttributes) : "")))
        {
        }
    }
}
