using System;

namespace Styx.Helpers
{
    public class FlagCheckedListBoxItem
    {
        public FlagCheckedListBoxItem(int v, string c)
        {
            this.value = v;
            this.caption = c;
        }

        public override string ToString()
        {
            return this.caption;
        }

        public bool IsFlag
        {
            get
            {
                return (this.value & (this.value - 1)) == 0;
            }
        }

        public bool IsMemberFlag(FlagCheckedListBoxItem composite)
        {
            return this.IsFlag && (this.value & composite.value) == this.value;
        }

        public int value;

        public string caption;
    }
}
