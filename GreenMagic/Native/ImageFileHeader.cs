namespace GreenMagic.Native
{
    public struct ImageFileHeader
    {
        public ushort Machine;

        public ushort NumberOfSections;

        public uint TimeDateStamp;

        public uint PointerToSymbolTable;

        public uint NumberOfSymbols;

        public ushort SizeOfOptionalHeader;

        public ushort Characteristics;
    }
}
