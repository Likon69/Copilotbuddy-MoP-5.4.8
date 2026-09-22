using System;
using System.Windows.Forms;

namespace Styx.Common
{
    public class Hotkey : IEquatable<Hotkey>
    {
        internal Hotkey(string name, Keys key, ModifierKeys modifierKeys, int id, Action<Hotkey> callback)
        {
            Name = name;
            Key = key;
            ModifierKeys = modifierKeys;
            Id = id;
            Callback = callback;
        }

        public Action<Hotkey> Callback { get; set; }
        public int Id { get; private set; }
        public Keys Key { get; private set; }
        public ModifierKeys ModifierKeys { get; private set; }
        public string Name { get; private set; }
        internal bool Registered { get; set; }

        public bool Equals(Hotkey other)
        {
            return !ReferenceEquals(null, other) &&
                   (ReferenceEquals(this, other) || Id == other.Id && string.Equals(Name, other.Name));
        }

        public override string ToString()
        {
            return Name + " [" + GetHotkeyDisplayString() + "]";
        }

        private string GetHotkeyDisplayString()
        {
            string display = "";
            if (ModifierKeys.HasFlag(ModifierKeys.Control))
                display += "Ctrl + ";
            if (ModifierKeys.HasFlag(ModifierKeys.Alt))
                display += "Alt + ";
            if (ModifierKeys.HasFlag(ModifierKeys.Shift))
                display += "Shift + ";
            return display + Key;
        }

        public override bool Equals(object obj)
        {
            return !ReferenceEquals(null, obj) &&
                   (ReferenceEquals(this, obj) || obj.GetType() == GetType() && Equals((Hotkey)obj));
        }

        public override int GetHashCode()
        {
            return (Id * 397) ^ (Name != null ? Name.GetHashCode() : 0);
        }

        public static bool operator ==(Hotkey left, Hotkey right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Hotkey left, Hotkey right)
        {
            return !Equals(left, right);
        }
    }
}
