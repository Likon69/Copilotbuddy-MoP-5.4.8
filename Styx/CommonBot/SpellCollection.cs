using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Styx.WoWInternals;

namespace Styx.CommonBot
{
    public class SpellCollection : Dictionary<string, WoWSpell>
    {
        internal SpellCollection()
        {
        }

        internal SpellCollection(int capacity)
            : base(capacity)
        {
        }

        internal SpellCollection(IEqualityComparer<string> comparer)
            : base(comparer)
        {
        }

        internal SpellCollection(int capacity, IEqualityComparer<string> comparer)
            : base(capacity, comparer)
        {
        }

        internal SpellCollection(IDictionary<string, WoWSpell> dictionary)
            : base(dictionary)
        {
        }

        internal SpellCollection(IDictionary<string, WoWSpell> dictionary, IEqualityComparer<string> comparer)
            : base(dictionary, comparer)
        {
        }

        protected SpellCollection(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public new WoWSpell this[string index]
        {
            get
            {
                if (!ContainsKey(index))
                {
                    throw new KeyNotFoundException("The specified spell could not be found: " + index);
                }
                return base[index];
            }
            set { base[index] = value; }
        }
    }
}
