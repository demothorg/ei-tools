using System.Runtime.InteropServices;

namespace EILib
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct MpFileHeader
    {
        public const uint MpFileSignature = 0xce4af672;

        public uint   Signature;
        public float  MaxZ;
        public uint   SectorsXCount;
        public uint   SectorsYCount;
        public uint   TexturesCount;
        public uint   TextureSize;
        public uint   TilesCount;
        public uint   TileSize;
        public ushort MaterialsCount;
        public uint   AnimTilesCount;
    }

    public enum EMaterialType : uint
    {
        Undefined = 0,
        Terrain = 1,
        WaterWithoutTexture = 2,
        Water = 3,
        Grass = 4
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct MpMaterial
    {
        public EMaterialType Type;
        public float R, G, B, A;     // Diffuse color of object
        public float SelfIllum;      // Self illumination of object
        public float WaveMultiplier;
        public float WarpSpeed;
        public float Reserved1;
        public float Reserved2;
        public float Reserved3;
    }

    public enum ETileType : uint
    {
        Grass = 0,
        Ground = 1,
        Stone = 2,
        Sand = 3,
        Rock = 4,
        Field = 5,
        Water = 6,
        Road = 7,
        Snow = 9,
        Ice = 10,
        Drygrass = 11,
        Snowballs = 12,
        Lava = 13,
        Swamp = 14,
        Undefined = 8,
        Highrock = 15,
        Last = 16
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct MpAnimTile
    {
        public ushort TileIndex;
        public ushort PhasesCount;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct SecFileHeader
    {
        public const uint SecFileSignature = 0xcf4bf774;

        public uint Signature;
        public byte Type;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct SecVertex
    {
        public sbyte  OffsetX;
        public sbyte  OffsetY;
        public ushort Z;
        public uint   PackedNormal;
    }
}
