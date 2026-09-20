using System;
using Styx.Resources;

namespace Styx.CommonBot.Profiles
{
    internal static class ProfileExceptionHelper
    {
        public static string GetTypeString<T>()
        {
            switch (Type.GetTypeCode(typeof(T)))
            {
                case TypeCode.Boolean:
                    return StyxResources.boolean;
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                    return StyxResources.integral;
                case TypeCode.Byte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return StyxResources.positive_integral;
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    return StyxResources.floating;
            }

            return StyxResources.Unknown;
        }

        public static string GetValidTagsString(params string[] validTags)
        {
            if (validTags != null && validTags.Length != 0)
                return string.Join(", ", validTags);

            return string.Empty;
        }
    }
}
