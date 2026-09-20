using System;

namespace Styx.Common
{
    public class HookDescription
    {
        public HookDescription(string name, string description = null)
        {
            Name = name;
            Description = description;
        }

        public string Name { get; set; }
        public string Description { get; set; }

        public bool Equals(HookDescription other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return other._guid.Equals(_guid);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != typeof(HookDescription))
                return false;
            return Equals((HookDescription)obj);
        }

        public override int GetHashCode()
        {
            return _guid.GetHashCode();
        }

        private Guid _guid = Guid.NewGuid();
    }
}
