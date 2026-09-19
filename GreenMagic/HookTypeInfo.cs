using System;

namespace GreenMagic
{
    internal class HookTypeInfo
    {
        public HookTypeInfo(string name, int copyAmount, int[] identifierBytes)
        {
            Name = name;
            CopyAmount = copyAmount;
            IdentifierBytes = identifierBytes;
        }

        public bool IdentifierBytesMatch(byte[] otherBytes)
        {
            for (int index = 0; index < IdentifierBytes.Length; index++)
            {
                if (IdentifierBytes[index] == -1)
                {
                    continue;
                }
                if (otherBytes[index] != IdentifierBytes[index])
                {
                    return false;
                }
            }
            return true;
        }

        public int CopyAmount
        {
            get { return _copyAmount; }
            set
            {
                if (value != 0 && value < 5)
                {
                    throw new ArgumentOutOfRangeException("value", "CopyAmount must be larger or equal to 5, because 'jmp addr' takes 5 bytes");
                }
                _copyAmount = value;
            }
        }

        public int[] IdentifierBytes;

        public string Name;

        public CopiedBytesTransformerDelegate CopiedBytesTransformer;

        private int _copyAmount;
    }
}
