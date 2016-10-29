using System;
using System.IO;

namespace EILib
{
    public class MprFile
    {
        public MprMaterial[]  Materials;
        public ETileType[]    TileTypes;
        public MprAnimTile[]  AnimTiles;

        public float MaxZ { get; private set; }
        public int SectorsXCount { get; private set; }
        public int SectorsYCount { get; private set; }
        public int TexturesCount { get; private set; }
        public int TextureSize { get; private set; }
        public int TileSize { get; private set; }

        public MprVertex[,]   LandVertices { get; private set; }
        public MprVertex[,]   WaterVertices { get; private set; }
        public MprTile[,]     LandTiles { get; private set; }
        public MprTile[,]     WaterTiles { get; private set; }
        public int[,]         WaterMaterials { get; private set; }

        public void Load(string path)
        {
            using (var f = new FileStream(path, FileMode.Open))
            {
                var files = ResFile.GetFiles(f);
                string zoneName = Path.GetFileNameWithoutExtension(path);

                // Load .mp file
                var mpFileEntry = files[zoneName + ".mp"];
                var mpFile = LoadMpFile(f, mpFileEntry.Position, mpFileEntry.Size);
                var header = mpFile.Header;
                int secXCount = (int)header.SectorsXCount;
                int secYCount = (int)header.SectorsYCount;
                int verXSize = secXCount * (SecFile.VerticesSideSize - 1) + 1;
                int verYSize = secYCount * (SecFile.VerticesSideSize - 1) + 1;
                int tilesXSize = secXCount * SecFile.TilesSideSize;
                int tilesYSize = secYCount * SecFile.TilesSideSize;

                MaxZ = mpFile.Header.MaxZ;
                SectorsXCount = (int)mpFile.Header.SectorsXCount;
                SectorsYCount = (int)mpFile.Header.SectorsYCount;
                TexturesCount = (int)mpFile.Header.TexturesCount;
                TextureSize   = (int)mpFile.Header.TextureSize;
                TileSize      = (int)mpFile.Header.TileSize;

                Materials = new MprMaterial[mpFile.Materials.Length];
                for (int i = 0; i < mpFile.Materials.Length; i++)
                    Materials[i] = new MprMaterial(mpFile.Materials[i]);

                TileTypes = (ETileType[])mpFile.TileTypes.Clone();

                AnimTiles = new MprAnimTile[mpFile.AnimTiles.Length];
                for (int i = 0; i < mpFile.AnimTiles.Length; i++)
                    AnimTiles[i] = new MprAnimTile(mpFile.AnimTiles[i]);

                // Load .sec files
                LandVertices   = new MprVertex[verXSize, verYSize];
                WaterVertices  = new MprVertex[verXSize, verYSize];
                LandTiles      = new MprTile[tilesXSize, tilesYSize];
                WaterTiles     = new MprTile[tilesXSize, tilesYSize];
                WaterMaterials = new int[tilesXSize, tilesYSize];

                for (int y = 0; y < mpFile.Header.SectorsYCount; y++)
                {
                    for (int x = 0; x < mpFile.Header.SectorsXCount; x++)
                    {
                        var secFileEntry = files[string.Format("{0}{1:D3}{2:D3}.sec", zoneName, x, y)];
                        var secFile = LoadSecFile(f, secFileEntry.Position, secFileEntry.Size);
                        PrepareSector(secFile, x, y);
                    }
                }
            }
        }

        public void Save(string path)
        {
            using (var f = new FileStream(path, FileMode.Create))
            {
                using (var res = new ResFile(f))
                {
                    var mpFile = new MpFile();
                    mpFile.Materials = new MpMaterial[Materials.Length];
                    for (int i = 0; i < Materials.Length; i++)
                        mpFile.Materials[i] = Materials[i].ToMpMaterial();

                    mpFile.TileTypes = (ETileType[])TileTypes.Clone();

                    mpFile.AnimTiles = new MpAnimTile[AnimTiles.Length];
                    for (int i = 0; i < AnimTiles.Length; i++)
                        mpFile.AnimTiles[i] = AnimTiles[i].ToMpAnimTile();

                    mpFile.Header = new MpFileHeader()
                    {
                        Signature = MpFileHeader.MpFileSignature,
                        MaxZ = this.MaxZ,
                        SectorsXCount  = (uint)this.SectorsXCount,
                        SectorsYCount  = (uint)this.SectorsYCount,
                        TexturesCount  = (uint)this.TexturesCount,
                        TextureSize    = (uint)this.TextureSize,
                        TilesCount     = (uint)this.TileTypes.Length,
                        TileSize       = (uint)this.TileSize,
                        MaterialsCount = (ushort)this.Materials.Length,
                        AnimTilesCount = (uint)this.AnimTiles.Length
                    };

                    string zoneName = Path.GetFileNameWithoutExtension(path);
                    res.AddFile(zoneName + ".mp", DateTime.Now);
                    SaveMpFile(f, mpFile);

                    for (int y = 0; y < mpFile.Header.SectorsYCount; y++)
                    {
                        for (int x = 0; x < mpFile.Header.SectorsXCount; x++)
                        {
                            res.AddFile(string.Format("{0}{1:D3}{2:D3}.sec", zoneName, x, y), DateTime.Now);
                            SaveSecFile(f, CreateSector(x, y));
                        }
                    }
                }
            }
        }

        private static MpFile LoadMpFile(Stream stream, long position, long size)
        {
            stream.Seek(position, SeekOrigin.Begin);

            MpFile mpFile = new MpFile();
            mpFile.Header = Utility.ReadStructure<MpFileHeader>(stream);

            mpFile.Materials = new MpMaterial[mpFile.Header.MaterialsCount];
            for (int i = 0; i < mpFile.Materials.Length; i++)
                mpFile.Materials[i] = Utility.ReadStructure<MpMaterial>(stream);

            var reader = new BinaryReader(stream);
            mpFile.TileTypes = new ETileType[mpFile.Header.TilesCount];
            for (int i = 0; i < mpFile.TileTypes.Length; i++)
                mpFile.TileTypes[i] = (ETileType)reader.ReadUInt32();

            mpFile.AnimTiles = new MpAnimTile[mpFile.Header.AnimTilesCount];
            for (int i = 0; i < mpFile.AnimTiles.Length; i++)
                mpFile.AnimTiles[i] = Utility.ReadStructure<MpAnimTile>(stream);

            long sizeAfter = stream.Position - position;
            if (size != sizeAfter)
                throw new InvalidDataException();

            return mpFile;
        }

        private static void SaveMpFile(Stream stream, MpFile mpFile)
        {
            Utility.WriteBytes(stream, Utility.GetBytes(mpFile.Header));

            for (int i = 0; i < mpFile.Materials.Length; i++)
                Utility.WriteBytes(stream, Utility.GetBytes(mpFile.Materials[i]));

            var writer = new BinaryWriter(stream);
            for (int i = 0; i < mpFile.TileTypes.Length; i++)
                writer.Write((uint)mpFile.TileTypes[i]);

            for (int i = 0; i < mpFile.AnimTiles.Length; i++)
                Utility.WriteBytes(stream, Utility.GetBytes(mpFile.AnimTiles[i]));
        }

        private static SecFile LoadSecFile(Stream stream, long position, long size)
        {
            stream.Seek(position, SeekOrigin.Begin);

            SecFile secFile = new SecFile();
            secFile.Header = Utility.ReadStructure<SecFileHeader>(stream);

            secFile.LandVertices = new SecVertex[SecFile.VerticesCount];
            for (int i = 0; i < secFile.LandVertices.Length; i++)
                secFile.LandVertices[i] = Utility.ReadStructure<SecVertex>(stream);

            secFile.WaterVertices = new SecVertex[SecFile.VerticesCount];
            if (secFile.Header.Type == 3)
            {
                for (int i = 0; i < secFile.WaterVertices.Length; i++)
                    secFile.WaterVertices[i] = Utility.ReadStructure<SecVertex>(stream);
            }
            else
            {
                for (int i = 0; i < secFile.WaterVertices.Length; i++)
                    secFile.WaterVertices[i] = new SecVertex { OffsetX = 0, OffsetY = 0, Z = 0, PackedNormal = 0 };
            }

            var reader = new BinaryReader(stream);
            secFile.LandTiles = new ushort[SecFile.TilesCount];
            for (int i = 0; i < secFile.LandTiles.Length; i++)
                secFile.LandTiles[i] = reader.ReadUInt16();

            secFile.WaterTiles = new ushort[SecFile.TilesCount];
            secFile.WaterAllow = new ushort[SecFile.TilesCount];
            if (secFile.Header.Type == 3)
            {
                for (int i = 0; i < secFile.WaterTiles.Length; i++)
                    secFile.WaterTiles[i] = reader.ReadUInt16();

                for (int i = 0; i < secFile.WaterAllow.Length; i++)
                    secFile.WaterAllow[i] = reader.ReadUInt16();
            }
            else
            {
                for (int i = 0; i < secFile.WaterTiles.Length; i++)
                {
                    secFile.WaterTiles[i] = 0;
                    secFile.WaterAllow[i] = 65535;
                }
            }

            long sizeAfter = stream.Position - position;
            if (size != sizeAfter)
                throw new InvalidDataException();

            return secFile;
        }

        private static void SaveSecFile(Stream stream, SecFile secFile)
        {
            Utility.WriteBytes(stream, Utility.GetBytes(secFile.Header));

            for (int i = 0; i < secFile.LandVertices.Length; i++)
                Utility.WriteBytes(stream, Utility.GetBytes(secFile.LandVertices[i]));

            if (secFile.Header.Type == 3)
            {
                for (int i = 0; i < secFile.WaterVertices.Length; i++)
                    Utility.WriteBytes(stream, Utility.GetBytes(secFile.WaterVertices[i]));
            }

            var writer = new BinaryWriter(stream);
            for (int i = 0; i < secFile.LandTiles.Length; i++)
                writer.Write((ushort)secFile.LandTiles[i]);

            if (secFile.Header.Type == 3)
            {
                for (int i = 0; i < secFile.WaterTiles.Length; i++)
                    writer.Write((ushort)secFile.WaterTiles[i]);

                for (int i = 0; i < secFile.WaterAllow.Length; i++)
                    writer.Write((ushort)secFile.WaterAllow[i]);
            }
        }

        private void PrepareSector(SecFile secFile, int secX, int secY)
        {
            int ofsVX = secX * (SecFile.VerticesSideSize - 1);
            int ofsVY = secY * (SecFile.VerticesSideSize - 1);
            int ofsTX = secX * SecFile.TilesSideSize;
            int ofsTY = secY * SecFile.TilesSideSize;

            for (int y = 0; y < SecFile.VerticesSideSize; y++)
            {
                for (int x = 0; x < SecFile.VerticesSideSize; x++)
                {
                    int vIndex = x + y * SecFile.VerticesSideSize;
                    LandVertices[x + ofsVX, y + ofsVY] = new MprVertex(secFile.LandVertices[vIndex]);
                    WaterVertices[x + ofsVX, y + ofsVY] = new MprVertex(secFile.WaterVertices[vIndex]);
                }
            }

            for (int y = 0; y < SecFile.TilesSideSize; y++)
            {
                for (int x = 0; x < SecFile.TilesSideSize; x++)
                {
                    int tIndex = x + y * SecFile.TilesSideSize;
                    LandTiles[x + ofsTX, y + ofsTY] = new MprTile(secFile.LandTiles[tIndex]);
                    WaterTiles[x + ofsTX, y + ofsTY] = new MprTile(secFile.WaterTiles[tIndex]);

                    int matIndex = secFile.WaterAllow[tIndex];
                    WaterMaterials[x + ofsTX, y + ofsTY] = matIndex < Materials.Length ? matIndex : -1;
                }
            }
        }

        private SecFile CreateSector(int secX, int secY)
        {
            int ofsVX = secX * (SecFile.VerticesSideSize - 1);
            int ofsVY = secY * (SecFile.VerticesSideSize - 1);
            int ofsTX = secX * SecFile.TilesSideSize;
            int ofsTY = secY * SecFile.TilesSideSize;

            var secFile = new SecFile();

            secFile.Header = new SecFileHeader()
            {
                Signature = SecFileHeader.SecFileSignature,
                Type = 3
            };

            secFile.LandVertices  = new SecVertex[SecFile.VerticesCount];
            secFile.WaterVertices = new SecVertex[SecFile.VerticesCount];
            secFile.LandTiles  = new ushort[SecFile.TilesCount];
            secFile.WaterTiles = new ushort[SecFile.TilesCount];
            secFile.WaterAllow = new ushort[SecFile.TilesCount];

            for (int y = 0; y < SecFile.VerticesSideSize; y++)
            {
                for (int x = 0; x < SecFile.VerticesSideSize; x++)
                {
                    int vIndex = x + y * SecFile.VerticesSideSize;
                    secFile.LandVertices[vIndex] = LandVertices[x + ofsVX, y + ofsVY].ToSecVertex();
                    secFile.WaterVertices[vIndex] = WaterVertices[x + ofsVX, y + ofsVY].ToSecVertex();
                }
            }

            bool hasWater = false;
            for (int y = 0; y < SecFile.TilesSideSize; y++)
            {
                for (int x = 0; x < SecFile.TilesSideSize; x++)
                {
                    int tIndex = x + y * SecFile.TilesSideSize;
                    secFile.LandTiles[tIndex] = LandTiles[x + ofsTX, y + ofsTY].ToSecTile();
                    secFile.WaterTiles[tIndex] = WaterTiles[x + ofsTX, y + ofsTY].ToSecTile();

                    int matIndex = WaterMaterials[x + ofsTX, y + ofsTY];
                    if (matIndex >= 0)
                        hasWater = true;

                    secFile.WaterAllow[tIndex] = (ushort)(matIndex >= 0 ? matIndex : ushort.MaxValue);
                    WaterMaterials[x + ofsTX, y + ofsTY] = matIndex < Materials.Length ? matIndex : -1;
                }
            }

            if (!hasWater)
            {
                secFile.Header.Type = 0;
                secFile.WaterVertices = null;
                secFile.WaterTiles = null;
                secFile.WaterAllow = null;
            }

            return secFile;
        }
    }

    public struct MprVertex
    {
        public int Z;
        public sbyte OffsetX;
        public sbyte OffsetY;
        public float NormalX;
        public float NormalY;
        public float NormalZ;

        internal MprVertex(SecVertex secVertex)
        {
            Z = secVertex.Z;
            OffsetX = secVertex.OffsetX;
            OffsetY = secVertex.OffsetY;

            // Unpack normal
            uint normal = secVertex.PackedNormal;
            NormalX = (((normal >> 11) & 0x7FF) - 1000.0f) / 1000.0f;
            NormalY = ((normal & 0x7FF) - 1000.0f) / 1000.0f;
            NormalZ = (normal >> 22) / 1000.0f;
        }

        internal SecVertex ToSecVertex()
        {
            var secVertex = new SecVertex();
            if (Z < 0 || Z > ushort.MaxValue)
                throw new ArgumentException();

            secVertex.Z = (ushort)Z;
            secVertex.OffsetX = OffsetX;
            secVertex.OffsetY = OffsetY;

            if (NormalX < -1 || NormalX > 1 || NormalY < -1 || NormalY > 1 || NormalZ < 0 || NormalZ > 1)
                throw new ArgumentException();

            uint normal = 0;
            normal |= (uint)Math.Floor(NormalX * 1000.0f + 1000.0f) << 11;
            normal |= (uint)Math.Floor(NormalY * 1000.0f + 1000.0f);
            normal |= (uint)Math.Floor(NormalZ * 1000.0f) << 22;
            secVertex.PackedNormal = normal;

            return secVertex;
        }
    }

    public struct MprTile
    {
        public int Index;
        public int Angle;

        internal MprTile(ushort secTile)
        {
            Index = secTile & 0x3FFF;
            Angle = secTile >> 14;
        }

        internal ushort ToSecTile()
        {
            if (Index < 0 || Index > 0x3FFF || Angle < 0 || Angle > 3)
                throw new ArgumentException();

            ushort secTile = 0;
            secTile = (ushort)(Index & 0x3FFFu);
            secTile |= (ushort)(Angle << 14);
            return secTile;
        }
    }

    public struct MprAnimTile
    {
        public ushort TileIndex;
        public ushort PhasesCount;

        internal MprAnimTile(MpAnimTile mpAnimTile)
        {
            TileIndex = mpAnimTile.TileIndex;
            PhasesCount = mpAnimTile.PhasesCount;
        }

        internal MpAnimTile ToMpAnimTile()
        {
            var mpAnimTile = new MpAnimTile();
            mpAnimTile.TileIndex = TileIndex;
            mpAnimTile.PhasesCount = PhasesCount;
            return mpAnimTile;
        }
    }

    public struct MprMaterial
    {
        public EMaterialType Type;
        public float R, G, B, A;     // Diffuse color of object
        public float SelfIllum;      // Self illumination of object
        public float WaveMultiplier;
        public float WarpSpeed;

        internal MprMaterial(MpMaterial mat)
        {
            Type = mat.Type;
            R = mat.R;
            G = mat.G;
            B = mat.B;
            A = mat.A;
            SelfIllum = mat.SelfIllum;
            WaveMultiplier = mat.WaveMultiplier;
            WarpSpeed = mat.WarpSpeed;
        }

        internal MpMaterial ToMpMaterial()
        {
            var mat = new MpMaterial();
            mat.Type = Type;
            mat.R = R;
            mat.G = G;
            mat.B = B;
            mat.A = A;
            mat.SelfIllum = SelfIllum;
            mat.WaveMultiplier = WaveMultiplier;
            mat.WarpSpeed = WarpSpeed;
            mat.Reserved1 = mat.Reserved2 = mat.Reserved3 = 0;
            return mat;
        }
    }

    internal struct MpFile
    {
        public MpFileHeader Header;
        public MpMaterial[] Materials;
        public ETileType[]  TileTypes;
        public MpAnimTile[] AnimTiles;
    }

    internal struct SecFile
    {
        public const int VerticesCount = 33 * 33;
        public const int VerticesSideSize = 33;
        public const int TilesCount = 16 * 16;
        public const int TilesSideSize = 16;

        public SecFileHeader Header;
        public SecVertex[]   LandVertices;
        public SecVertex[]   WaterVertices;
        public ushort[]      LandTiles;
        public ushort[]      WaterTiles;
        public ushort[]      WaterAllow;
    }
}
