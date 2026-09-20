using System;
using System.ComponentModel;
using System.Resources;

namespace Styx.Localization
{
    public class LocalizedDescriptionAttribute : DescriptionAttribute
    {
        public LocalizedDescriptionAttribute(string resourceName)
        {
            _resourceName = resourceName;
            _resourceManager = Globalization.ResourceManager;
        }

        public override string Description
        {
            get { return _resourceManager.GetString(_resourceName); }
        }

        private readonly string _resourceName;

        private readonly ResourceManager _resourceManager;
    }
}
