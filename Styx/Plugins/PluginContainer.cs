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
                    try
                    {
                        this.Plugin.Dispose();
                        this.Plugin.OnDisable();
                    }
                    catch (Exception ex)
                    {
                        Styx.Common.Logging.WriteDiagnostic("Exception while disabling plugin " + this.Name + ": " + ex);
                    }
                    this._enabled = value;
                }
                else
                {
                    try
                    {
                        this.Plugin.Initialize();
                    }
                    catch (Exception ex)
                    {
                        Styx.Common.Logging.WriteDiagnostic("Exception thrown when initializing " + this.Name + ". Plugin is now disabled. Stack trace is as follows.");
                        Styx.Common.Logging.WriteException(ex);
                        this._enabled = false;
                        return;
                    }
                    try
                    {
                        this.Plugin.OnEnable();
                    }
                    catch (Exception ex)
                    {
                        Styx.Common.Logging.WriteDiagnostic("Exception while enabling plugin " + this.Name + ": " + ex);
                    }
                    this._enabled = value;
                }
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
