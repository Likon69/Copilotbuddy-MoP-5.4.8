namespace GreenMagic.Native
{
    public struct ImageExportDirectory
    {
        public uint Characteristics;

        public uint TimeDateStamp;

        public ushort MajorVersion;

        public ushort MinorVersion;

        public uint Name;

        public uint Base;

        public uint NumberOfFunctions;

        public uint NumberOfNames;

        public uint AddressOfFunctions;

        public uint AddressOfNames;

        public uint AddressOfNameOrdinals;
    }
}
