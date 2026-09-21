using System;

namespace Styx.Plugins
{
    public abstract class HBPlugin
    {
        public abstract void Pulse();

        public virtual void OnButtonPress()
        {
        }

        public virtual bool WantButton
        {
            get
            {
                return false;
            }
        }

        public virtual string ButtonText
        {
            get
            {
                return "Settings";
            }
        }

        public abstract string Name { get; }

        public abstract string Author { get; }

        public abstract Version Version { get; }

        [Obsolete("Use OnEnable instead")]
        public virtual void Initialize()
        {
        }

        [Obsolete("Use OnDisable instead")]
        public virtual void Dispose()
        {
        }

        public virtual void OnEnable()
        {
        }

        public virtual void OnDisable()
        {
        }
    }
}
