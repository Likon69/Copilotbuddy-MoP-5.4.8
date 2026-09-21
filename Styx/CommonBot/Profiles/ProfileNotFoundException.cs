using System;

namespace Styx.CommonBot.Profiles
{
    public class ProfileNotFoundException : ProfileException
    {
        public ProfileNotFoundException(string path)
            : base(string.Format("Unable to load new profile, profile at path \"{0}\" couldn't be found", path))
        {
        }
    }
}
