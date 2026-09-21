using System;
using Styx.Common;

namespace Styx.Plugins
{
    public class PluginWrapper : IDisposable
    {
        public PluginWrapper(HBPlugin plugin)
        {
            if (plugin == null)
            {
                throw new ArgumentNullException("plugin");
            }
            this._hbplugin = plugin;
        }

        public string Name
        {
            get
            {
                string text;
                try
                {
                    text = this._hbplugin.Name;
                }
                catch
                {
                    text = "EXCEPTION THROWN";
                }
                return text;
            }
        }

        public string ButtonText
        {
            get
            {
                string text;
                try
                {
                    text = this._hbplugin.ButtonText;
                }
                catch
                {
                    text = "EXCEPTION THROWN";
                }
                return text;
            }
        }

        public bool WantButton
        {
            get
            {
                bool flag;
                try
                {
                    flag = this._hbplugin.WantButton;
                }
                catch
                {
                    flag = false;
                }
                return flag;
            }
        }

        public void OnButtonPress()
        {
            try
            {
                this._hbplugin.OnButtonPress();
            }
            catch (Exception ex)
            {
                Styx.Common.Logging.WriteDiagnostic("Plugin {0} threw an exception in 'OnButtonPress'! Exception:", new object[] { this._hbplugin.Name });
                Styx.Common.Logging.WriteException(ex);
            }
        }

        public void Pulse()
        {
            try
            {
                this._hbplugin.Pulse();
            }
            catch (Exception ex)
            {
                Styx.Common.Logging.WriteDiagnostic("Plugin {0} threw an exception in 'Pulse'! Exception:", new object[] { this._hbplugin.Name });
                Styx.Common.Logging.WriteException(ex);
            }
        }

        public void OnEnable()
        {
            try
            {
                this._hbplugin.OnEnable();
            }
            catch (Exception ex)
            {
                Styx.Common.Logging.WriteDiagnostic("Plugin {0} threw an exception in OnEnable", new object[] { this._hbplugin.Name });
                Styx.Common.Logging.WriteException(ex);
            }
        }

        public void OnDisable()
        {
            try
            {
                this._hbplugin.OnDisable();
            }
            catch (Exception ex)
            {
                Styx.Common.Logging.WriteDiagnostic("Plugin {0} threw an exception in OnDisable", new object[] { this._hbplugin.Name });
                Styx.Common.Logging.WriteException(ex);
            }
        }

        public void Initialize()
        {
            try
            {
                this._hbplugin.Initialize();
            }
            catch (Exception ex)
            {
                Styx.Common.Logging.WriteDiagnostic("Plugin {0} threw an exception in Initialize", new object[] { this._hbplugin.Name });
                Styx.Common.Logging.WriteException(ex);
            }
        }

        public static implicit operator PluginWrapper(HBPlugin plugin)
        {
            return new PluginWrapper(plugin);
        }

        public void Dispose()
        {
            try
            {
                this._hbplugin.Dispose();
            }
            catch
            {
            }
        }

        private readonly HBPlugin _hbplugin;
    }
}
