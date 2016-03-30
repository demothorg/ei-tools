using System.Runtime.InteropServices;

namespace EILib
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct ResFileHeader
    {
        public const uint ResFileSignature = 0x019CE23C;

        public uint Signature;
        public uint TableSize;
        public uint TableOffset;
        public uint NamesLength;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct ResFileHashTableEntry
    {
        public uint   NextIndex;
        public uint   DataSize;
        public uint   DataOffset;
        public uint   LastWriteTime;
        public ushort NameLength;
        public uint   NameOffset;
    }
}
