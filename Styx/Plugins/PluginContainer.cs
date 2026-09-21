using System;

namespace Styx.Plugins
{
    public class PluginContainer : IComparable<PluginContainer>
    {
        public PluginWrapper Plugin { get; private set; }

        public string Name { get; private set; }

        public Version Version { get; private set; }

        public string Author { get; private set; }

        public bool WantButton
        {
            get
            {
                return this.Plugin.WantButton;
            }
        }

        public string ButtonText
        {
            get
            {
                return this.Plugin.ButtonText;
            }
        }

        public bool Enabled
        {
            get
            {
                return this._enabled;
            }
            set
            {
                if (value == this._enabled)
                {
                    return;
                }
                if (!value)
                {
                    this.Plugin.Dispose();
                    this.Plugin.OnDisable();
                }
                else
                {
                    this.Plugin.Initialize();
                    this.Plugin.OnEnable();
                }
                this._enabled = value;
            }
        }

        public PluginContainer(HBPlugin plugin)
        {
            if (plugin == null)
            {
                throw new ArgumentNullException("plugin");
            }
            this.Plugin = plugin;
            this.Name = plugin.Name;
            this.Version = plugin.Version;
            this.Author = plugin.Author;
        }

        public int CompareTo(PluginContainer other)
        {
            return string.Compare(this.Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }

        private bool _enabled;
    }
}
