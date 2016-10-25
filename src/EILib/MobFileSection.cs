using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace EILib
{
    public class MobFileSection
    {
        public ESectionId Id { get; private set; }

        public ESectionType Type
        {
            get
            {
                if (Id == ESectionId.UNKNOWN && Owner == null)
                    return ESectionType.Record;
                else if (_sectionInfos.ContainsKey(Id))
                    return _sectionInfos[Id].Type;
                else
                    return ESectionType.Unknown;
            }
        }

        public int Size
        {
            get
            {
                int size = Owner == null ? 0 : 8; // Size of header
                if (_data != null)
                    return size + _data.Length;

                if (Type != ESectionType.Record)
                    return size;

                Debug.Assert(Items != null);
                foreach (var i in Items)
                    size += i.Size;

                return size;
            }
        }

        public MobFileSection Owner;
        public List<MobFileSection> Items { get; private set; }
        private byte[] _data;

        public MobFileSection(byte[] data, MobFileSection owner)
            : this()
        {
            InitSection(data, owner);
            if (Type == ESectionType.Record)
                ReadSubsections();
        }

        // Crypt/decrypt for raw byte array
        public static void CryptData(byte[] src)
        {
            uint tmpKey, i, key = BitConverter.ToUInt32(src, 0);
            for (i = 4; i < src.Length; i++)
            {
                tmpKey = ((((key * 13) << 4) + key) << 8) - key;
                key += (tmpKey << 2) + 2531011;
                tmpKey = key >> 16;
                src[i] ^= (byte)tmpKey;
            }
        }

        public static void EncryptString(string src, ref byte[] dest)
        {
            var data = Encoding.GetEncoding(1251).GetBytes(src);
            Array.Resize(ref dest, data.Length + 4);
            data.CopyTo(dest, 4);
            CryptData(dest);
        }

        public static string DecryptString(byte[] src)
        {
            var tmp = (byte[])src.Clone();
            CryptData(tmp);
            return Encoding.GetEncoding(1251).GetString(tmp, 4, tmp.Length - 4);
        }

        public MobFileSection Clone(MobFileSection owner = null)
        {
            var result = new MobFileSection()
            {
                Owner = owner,
                Id = this.Id
            };

            if (_data != null)
            {
                result._data = (byte[])_data.Clone();
                return result;
            }

            Debug.Assert(Items != null && Type == ESectionType.Record);
            result.Items = new List<MobFileSection>();
            foreach (var i in Items)
                result.Items.Add(i.Clone(result));

            return result;
        }

        public byte[] GetData()
        {
            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);

            // Write header
            if (Owner != null)
            {
                writer.Write((uint)Id);
                writer.Write((uint)0); // Update it later
            }

            if (_data == null)
            {
                Debug.Assert(Items != null && Type == ESectionType.Record);
                foreach (var i in Items)
                    writer.Write(i.GetData());
            }
            else
                writer.Write(_data);

            // Update size in header
            if (Owner != null)
            {
                uint size = (uint)stream.Position;
                stream.Seek(4, SeekOrigin.Begin);
                writer.Write(size);
            }

            return stream.ToArray();
        }

        private void CheckType(params ESectionType[] types)
        {
            foreach (var type in types)
            {
                if (type != Type)
                    throw new InvalidOperationException();
            }
        }

        public string ValString
        {
            get
            {
                CheckType(ESectionType.String, ESectionType.StringEncrypted);
                if (Type == ESectionType.String)
                    return Encoding.GetEncoding(1251).GetString(_data, 0, _data.Length);
                else
                    return DecryptString(_data);
            }
            set
            {
                CheckType(ESectionType.String, ESectionType.StringEncrypted);
                if (Type == ESectionType.String)
                    _data = Encoding.GetEncoding(1251).GetBytes(value);
                else
                    EncryptString(value, ref _data);
            }
        }

        public byte ValByte
        {
            get
            {
                CheckType(ESectionType.Byte);
                return _data[0];
            }
            set
            {
                CheckType(ESectionType.Byte);
                _data = new byte[] { value };
            }
        }

        public uint ValDword
        {
            get
            {
                CheckType(ESectionType.Dword);
                return BitConverter.ToUInt32(_data, 0);
            }
            set
            {
                CheckType(ESectionType.Dword);
                _data = BitConverter.GetBytes(Convert.ToUInt32(value));
            }
        }

        public float ValFloat
        {
            get
            {
                CheckType(ESectionType.Float);
                return BitConverter.ToSingle(_data, 0);
            }
            set
            {
                CheckType(ESectionType.Float);
                _data = BitConverter.GetBytes(Convert.ToSingle(value));
            }
        }

        public Plot ValPlot
        {
            get
            {
                CheckType(ESectionType.Plot);
                return new Plot()
                {
                    X = BitConverter.ToSingle(_data, 0),
                    Y = BitConverter.ToSingle(_data, 4),
                    Z = BitConverter.ToSingle(_data, 8)
                };
            }
            set
            {
                CheckType(ESectionType.Plot);
                _data = new byte[12];
                BitConverter.GetBytes(Convert.ToSingle(value.X)).CopyTo(_data, 0);
                BitConverter.GetBytes(Convert.ToSingle(value.Y)).CopyTo(_data, 4);
                BitConverter.GetBytes(Convert.ToSingle(value.Z)).CopyTo(_data, 8);
            }
        }

        public Quaternion ValQuaternion
        {
            get
            {
                CheckType(ESectionType.Quaternion);
                return new Quaternion()
                {
                    W = BitConverter.ToSingle(_data, 0),
                    X = BitConverter.ToSingle(_data, 4),
                    Y = BitConverter.ToSingle(_data, 8),
                    Z = BitConverter.ToSingle(_data, 12)
                };
            }
            set
            {
                CheckType(ESectionType.Quaternion);
                _data = new byte[16];
                BitConverter.GetBytes(Convert.ToSingle(value.W)).CopyTo(_data, 0);
                BitConverter.GetBytes(Convert.ToSingle(value.X)).CopyTo(_data, 4);
                BitConverter.GetBytes(Convert.ToSingle(value.Y)).CopyTo(_data, 8);
                BitConverter.GetBytes(Convert.ToSingle(value.Z)).CopyTo(_data, 12);
            }
        }

        private MobFileSection()
        {
            Items = null;
            Owner = null;
            Id = ESectionId.UNKNOWN;
        }

        private static Dictionary<ESectionId, SectionInfo> PrepareSectionDicionary()
        {
            var result = new Dictionary<ESectionId, SectionInfo>();
            foreach (var i in _knownSections)
                result[i.Id] = i;

            return result;
        }

        private void InitSection(byte[] data, MobFileSection owner)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            if (data.Length < 8)
                throw new ArgumentException("Invalid data", "data");

            Items = null;
            Owner = owner;
            Id = ESectionId.UNKNOWN;
            if (owner == null)
            {
                // Root section. Record without header
                _data = (byte[])data.Clone();
                return;
            }

            uint id = BitConverter.ToUInt32(data, 0);
            uint size = BitConverter.ToUInt32(data, 4);
            if (size > data.Length)
                throw new ArgumentException("Invalid data", "data");

            _data = new byte[size - 8];
            Array.Copy(data, 8, _data, 0, _data.Length);
            Id = (ESectionId)id;
        }

        private void ReadSubsections()
        {
            if (Items != null)
                return; // Already readed

            if (Type != ESectionType.Record)
                throw new InvalidOperationException();

            Items = new List<MobFileSection>();
            int offset = 0, size = _data.Length;
            while (offset + 8 <= size)
            {
                uint secSize = BitConverter.ToUInt32(_data, offset + 4);
                if (offset + secSize > size)
                    throw new InvalidOperationException();

                var secData = new byte[secSize];
                Array.Copy(_data, offset, secData, 0, secData.Length);

                var section = new MobFileSection(secData, this);
                Items.Add(section);

                offset += (int)secSize;
            }

            if (offset != size)
                throw new InvalidOperationException();

            _data = null;
        }

        private class SectionInfo
        {
            public ESectionId Id { get; private set; }
            public ESectionType Type { get; private set; }
            public string Name { get; private set; }
            public SectionInfo(ESectionId id, ESectionType type, string name)
            {
                Id = id;
                Type = type;
                Name = name;
            }
        }

        static MobFileSection()
        {
            _sectionInfos = PrepareSectionDicionary();
        }

        private static Dictionary<ESectionId, SectionInfo> _sectionInfos;
        private static SectionInfo[] _knownSections = new SectionInfo[]
        {
            new SectionInfo(ESectionId.UNKNOWN, ESectionType.Unknown, ""),
            new SectionInfo(ESectionId.WORLD_SET, ESectionType.Record, ""),
            new SectionInfo(ESectionId.OBJ_DEF_LOGIC, ESectionType.Null, ""),
            new SectionInfo(ESectionId.PR_OBJECTDBFILE, ESectionType.Null, ""),
            new SectionInfo(ESectionId.DIR_NAME, ESectionType.String, ""),
            new SectionInfo(ESectionId.DIPLOMATION, ESectionType.Record, ""),
            new SectionInfo(ESectionId.WS_WIND_DIR, ESectionType.Plot, ""),
            new SectionInfo(ESectionId.OBJECTSECTION, ESectionType.Record, ""),
            new SectionInfo(ESectionId.OBJROTATION, ESectionType.Quaternion, ""),
            new SectionInfo(ESectionId.OBJ_PLAYER, ESectionType.Byte, ""),
            new SectionInfo(ESectionId.DIR_NINST, ESectionType.Dword, ""),
            new SectionInfo(ESectionId.DIPLOMATION_FOF, ESectionType.Diplomacy, ""),
            new SectionInfo(ESectionId.OBJECTDBFILE, ESectionType.Record, ""),
            new SectionInfo(ESectionId.LIGHT_SECTION, ESectionType.Null, ""),
            new SectionInfo(ESectionId.WS_WIND_STR, ESectionType.Float, ""),
            new SectionInfo(ESectionId.OBJECT, ESectionType.Record, ""),
            new SectionInfo(ESectionId.OBJTEXTURE, ESectionType.Null, ""),
            new SectionInfo(ESectionId.OBJ_PARENT_ID, ESectionType.Dword, ""),
            new SectionInfo(ESectionId.SOUND_SECTION, ESectionType.Null, ""),
            new SectionInfo(ESectionId.SOUND_RESNAME, ESectionType.StringArray, ""),
            new SectionInfo(ESectionId.PARTICL_SECTION, ESectionType.Null, ""),
            new SectionInfo(ESectionId.DIR_PARENT_FOLDER, ESectionType.Dword, ""),
            new SectionInfo(ESectionId.SEC_RANGE, ESectionType.Record, ""),
            new SectionInfo(ESectionId.DIPLOMATION_PL_NAMES, ESectionType.StringArray, ""),
            new SectionInfo(ESectionId.LIGHT, ESectionType.Record, ""),
            new SectionInfo(ESectionId.WS_TIME, ESectionType.Float, ""),
            new SectionInfo(ESectionId.NID, ESectionType.Dword, ""),
            new SectionInfo(ESectionId.OBJCOMPLECTION, ESectionType.Plot, ""),
            new SectionInfo(ESectionId.OBJ_USE_IN_SCRIPT, ESectionType.Byte, ""),
            new SectionInfo(ESectionId.SOUND, ESectionType.Record, ""),
            new SectionInfo(ESectionId.SOUND_RANGE2, ESectionType.Dword, ""),
            new SectionInfo(ESectionId.PARTICL, ESectionType.Record, ""),
            new SectionInfo(ESectionId.DIR_TYPE, ESectionType.Byte, ""),
            new SectionInfo(ESectionId.MAIN_RANGE, ESectionType.Record, ""),
            new SectionInfo(ESectionId.VSS_BS_COMMANDS, ESectionType.StringArray, ""),
            new SectionInfo(ESectionId.LIGHT_RANGE, ESectionType.Float, ""),
            new SectionInfo(ESectionId.WS_AMBIENT, ESectionType.Float, ""),
            new SectionInfo(ESectionId.OBJTYPE, ESectionType.Dword, ""),
            new SectionInfo(ESectionId.OBJBODYPARTS, ESectionType.StringArray, ""),
            new SectionInfo(ESectionId.OBJ_IS_SHADOW, ESectionType.Byte, ""),
            new SectionInfo(ESectionId.SOUND_ID, ESectionType.Dword, ""),
            new SectionInfo(ESectionId.PARTICL_ID, ESectionType.Dword, ""),
            new SectionInfo(ESectionId.RANGE, ESectionType.Record, ""),
            new SectionInfo(ESectionId.VSS_SECTION, ESectionType.Record, ""),
            new SectionInfo(ESectionId.VSS_ISSTART, ESectionType.Byte, ""),
            new SectionInfo(ESectionId.VSS_CUSTOM_SRIPT, ESectionType.String, ""),
            new SectionInfo(ESectionId.LIGHT_NAME, ESectionType.String, ""),
            new SectionInfo(ESectionId.WS_SUN_LIGHT, ESectionType.Float, ""),
            new SectionInfo(ESectionId.OBJNAME, ESectionType.String, ""),
            new SectionInfo(ESectionId.PARENTTEMPLATE, ESectionType.String, ""),
            new SectionInfo(ESectionId.OBJ_R, ESectionType.Null, ""),
            new SectionInfo(ESectionId.SOUND_POSITION, ESectionType.Plot, ""),
            new SectionInfo(ESectionId.SOUND_AMBIENT, ESectionType.Byte, ""),
            new SectionInfo(ESectionId.PARTICL_POSITION, ESectionType.Plot, ""),
            new SectionInfo(ESectionId.UNIT, ESectionType.Record, ""),
            new SectionInfo(ESectionId.UNIT_NEED_IMPORT, ESectionType.Byte, ""),
            new SectionInfo(ESectionId.VSS_TRIGER, ESectionType.Record, ""),
            new SectionInfo(ESectionId.VSS_LINK, ESectionType.Record, ""),
            new SectionInfo(ESectionId.LIGHT_POSITION, ESectionType.Plot, ""),
            new SectionInfo(ESectionId.OBJINDEX, ESectionType.Null, ""),
            new SectionInfo(ESectionId.OBJCOMMENTS, ESectionType.String, ""),
            new SectionInfo(ESectionId.OBJ_QUEST_INFO, ESectionType.String, ""),
            new SectionInfo(ESectionId.SOUND_RANGE, ESectionType.Dword, ""),
            new SectionInfo(ESectionId.SOUND_IS_MUSIC, ESectionType.Byte, ""),
            new SectionInfo(ESectionId.PARTICL_COMMENTS, ESectionType.String, ""),
            new SectionInfo(ESectionId.MAGIC_TRAP, ESectionType.Record, ""),
            new SectionInfo(ESectionId.UNIT_R, ESectionType.Null, ""),
            new SectionInfo(ESectionId.UNIT_LOGIC, ESectionType.Record, ""),
            new SectionInfo(ESectionId.UNIT_LOGIC_WAIT, ESectionType.Float, ""),
            new SectionInfo(ESectionId.VSS_CHECK, ESectionType.Record, ""),
            new SectionInfo(ESectionId.VSS_GROUP, ESectionType.String, ""),
            new SectionInfo(ESectionId.LIGHT_ID, ESectionType.Dword, ""),
            new SectionInfo(ESectionId.OBJTEMPLATE, ESectionType.String, ""),
            new SectionInfo(ESectionId.SOUND_NAME, ESectionType.String, ""),
            new SectionInfo(ESectionId.PARTICL_NAME, ESectionType.String, ""),
            new SectionInfo(ESectionId.MIN_ID, ESectionType.Dword, ""),
            new SectionInfo(ESectionId.MT_DIPLOMACY, ESectionType.Dword, ""),
            new SectionInfo(ESectionId.LEVER, ESectionType.Record, ""),
            new SectionInfo(ESectionId.UNIT_PROTOTYPE, ESectionType.String, ""),
            new SectionInfo(ESectionId.UNIT_LOGIC_AGRESSIV, ESectionType.Null, ""),
            new SectionInfo(ESectionId.UNIT_LOGIC_ALARM_CONDITION, ESectionType.Byte, ""),
            new SectionInfo(ESectionId.GUARD_PT, ESectionType.Record, ""),
            new SectionInfo(ESectionId.VSS_PATH, ESectionType.Record, ""),
            new SectionInfo(ESectionId.VSS_IS_USE_GROUP, ESectionType.Byte, ""),
            new SectionInfo(ESectionId.LIGHT_SHADOW, ESectionType.Byte, ""),
            new SectionInfo(ESectionId.OBJPRIMTXTR, ESectionType.String, ""),
            new SectionInfo(ESectionId.SOUND_MIN, ESectionType.Dword, ""),
            new SectionInfo(ESectionId.PARTICL_TYPE, ESectionType.Dword, ""),
            new SectionInfo(ESectionId.MAX_ID, ESectionType.Dword, ""),
            new SectionInfo(ESectionId.MT_SPELL, ESectionType.String, ""),
            new SectionInfo(ESectionId.LEVER_SCIENCE_STATS, ESectionType.Null, ""),
            new SectionInfo(ESectionId.UNIT_ITEMS, ESectionType.Null, ""),
            new SectionInfo(ESectionId.UNIT_LOGIC_CYCLIC, ESectionType.Byte, ""),
            new SectionInfo(ESectionId.UNIT_LOGIC_HELP, ESectionType.Float, ""),
            new SectionInfo(ESectionId.GUARD_PT_POSITION, ESectionType.Plot, ""),
            new SectionInfo(ESectionId.ACTION_PT, ESectionType.Record, ""),
            new SectionInfo(ESectionId.VSS_ID, ESectionType.Dword, ""),
            new SectionInfo(ESectionId.VSS_VARIABLE, ESectionType.Record, ""),
            new SectionInfo(ESectionId.LIGHT_COLOR, ESectionType.Plot, ""),
            new SectionInfo(ESectionId.OBJSECTXTR, ESectionType.String, ""),
            new SectionInfo(ESectionId.SOUND_MAX, ESectionType.Dword, ""),
            new SectionInfo(ESectionId.PARTICL_SCALE, ESectionType.Float, ""),
            new SectionInfo(ESectionId.AIGRAPH, ESectionType.AiGraph, ""),
            new SectionInfo(ESectionId.MT_AREAS, ESectionType.AreaArray, ""),
            new SectionInfo(ESectionId.LEVER_CUR_STATE, ESectionType.Byte, ""),
            new SectionInfo(ESectionId.UNIT_STATS, ESectionType.UnitStats, ""),
            new SectionInfo(ESectionId.UNIT_LOGIC_MODEL, ESectionType.Dword, ""),
            new SectionInfo(ESectionId.UNIT_LOGIC_ALWAYS_ACTIVE, ESectionType.Byte, ""),
            new SectionInfo(ESectionId.GUARD_PT_ACTION, ESectionType.Null, ""),
            new SectionInfo(ESectionId.ACTION_PT_LOOK_PT, ESectionType.Plot, ""),
            new SectionInfo(ESectionId.TORCH, ESectionType.Record, ""),
            new SectionInfo(ESectionId.VSS_RECT, ESectionType.Rectangle, ""),
            new SectionInfo(ESectionId.VSS_BS_CHECK, ESectionType.StringArray, ""),
            new SectionInfo(ESectionId.LIGHT_COMMENTS, ESectionType.String, ""),
            new SectionInfo(ESectionId.OBJPOSITION, ESectionType.Plot, ""),
            new SectionInfo(ESectionId.SOUND_COMMENTS, ESectionType.String, ""),
            new SectionInfo(ESectionId.MT_TARGETS, ESectionType.Plot2DArray, ""),
            new SectionInfo(ESectionId.LEVER_TOTAL_STATE, ESectionType.Byte, ""),
            new SectionInfo(ESectionId.UNIT_QUEST_ITEMS, ESectionType.StringArray, ""),
            new SectionInfo(ESectionId.UNIT_LOGIC_GUARD_R, ESectionType.Float, ""),
            new SectionInfo(ESectionId.UNIT_LOGIC_AGRESSION_MODE, ESectionType.Byte, ""),
            new SectionInfo(ESectionId.ACTION_PT_WAIT_SEG, ESectionType.Dword, ""),
            new SectionInfo(ESectionId.TORCH_STRENGHT, ESectionType.Float, ""),
            new SectionInfo(ESectionId.VSS_SRC_ID, ESectionType.Dword, ""),
            new SectionInfo(ESectionId.SOUND_VOLUME, ESectionType.Null, ""),
            new SectionInfo(ESectionId.MT_CAST_INTERVAL, ESectionType.Dword, ""),
            new SectionInfo(ESectionId.LEVER_IS_CYCLED, ESectionType.Byte, ""),
            new SectionInfo(ESectionId.UNIT_QUICK_ITEMS, ESectionType.StringArray, ""),
            new SectionInfo(ESectionId.UNIT_LOGIC_GUARD_PT, ESectionType.Plot, ""),
            new SectionInfo(ESectionId.ACTION_PT_TURN_SPEED, ESectionType.Dword, ""),
            new SectionInfo(ESectionId.TORCH_PTLINK, ESectionType.Plot, ""),
            new SectionInfo(ESectionId.VSS_DST_ID, ESectionType.Dword, ""),
            new SectionInfo(ESectionId.LEVER_CAST_ONCE, ESectionType.Byte, ""),
            new SectionInfo(ESectionId.UNIT_SPELLS, ESectionType.StringArray, ""),
            new SectionInfo(ESectionId.UNIT_LOGIC_NALARM, ESectionType.Byte, ""),
            new SectionInfo(ESectionId.ACTION_PT_FLAGS, ESectionType.Byte, ""),
            new SectionInfo(ESectionId.TORCH_SOUND, ESectionType.String, ""),
            new SectionInfo(ESectionId.VSS_TITLE, ESectionType.String, ""),
            new SectionInfo(ESectionId.LEVER_SCIENCE_STATS_NEW, ESectionType.LeverStats, ""),
            new SectionInfo(ESectionId.UNIT_WEAPONS, ESectionType.StringArray, ""),
            new SectionInfo(ESectionId.UNIT_LOGIC_USE, ESectionType.Byte, ""),
            new SectionInfo(ESectionId.VSS_COMMANDS, ESectionType.String, ""),
            new SectionInfo(ESectionId.DIRICTORY_ELEMENTS, ESectionType.Record, ""),
            new SectionInfo(ESectionId.LEVER_IS_DOOR, ESectionType.Byte, ""),
            new SectionInfo(ESectionId.UNIT_ARMORS, ESectionType.StringArray, ""),
            new SectionInfo(ESectionId.UNIT_LOGIC_REVENGE, ESectionType.Null, ""),
            new SectionInfo(ESectionId.DIRICTORY, ESectionType.Record, ""),
            new SectionInfo(ESectionId.SS_TEXT_OLD, ESectionType.String, ""),
            new SectionInfo(ESectionId.LEVER_RECALC_GRAPH, ESectionType.Byte, ""),
            new SectionInfo(ESectionId.UNIT_LOGIC_FEAR, ESectionType.Null, ""),
            new SectionInfo(ESectionId.SC_OBJECTDBFILE, ESectionType.Null, ""),
            new SectionInfo(ESectionId.FOLDER, ESectionType.Record, ""),
            new SectionInfo(ESectionId.SS_TEXT, ESectionType.StringEncrypted, "")
        };
    }

    public enum ESectionType
    {
        Unknown,

        // General types
        Null,
        Record,
        Byte,
        Dword,
        Float,
        String,
        StringArray,
        Plot,

        // Custom types, structures
        AiGraph,
        StringEncrypted,
        Diplomacy,
        LeverStats,
        Plot2DArray,
        AreaArray,
        Quaternion,
        UnitStats,
        Rectangle
    }

    public enum ESectionId : uint
    {
        UNKNOWN = 0xFFFFFFFF,
        WORLD_SET = 0x0000ABD0,
        OBJ_DEF_LOGIC = 0x0000B010,
        PR_OBJECTDBFILE = 0x0000D000,
        DIR_NAME = 0x0000E002,
        DIPLOMATION = 0xDDDDDDD1,
        WS_WIND_DIR = 0x0000ABD1,
        OBJECTSECTION = 0x0000B000,
        OBJROTATION = 0x0000B00A,
        OBJ_PLAYER = 0x0000B011,
        DIR_NINST = 0x0000E003,
        DIPLOMATION_FOF = 0xDDDDDDD2,
        OBJECTDBFILE = 0x0000A000,
        LIGHT_SECTION = 0x0000AA00,
        WS_WIND_STR = 0x0000ABD2,
        OBJECT = 0x0000B001,
        OBJTEXTURE = 0x0000B00B,
        OBJ_PARENT_ID = 0x0000B012,
        SOUND_SECTION = 0x0000CC00,
        SOUND_RESNAME = 0x0000CC0A,
        PARTICL_SECTION = 0x0000DD00,
        DIR_PARENT_FOLDER = 0x0000E004,
        SEC_RANGE = 0x0000FF00,
        DIPLOMATION_PL_NAMES = 0xDDDDDDD3,
        LIGHT = 0x0000AA01,
        WS_TIME = 0x0000ABD3,
        NID = 0x0000B002,
        OBJCOMPLECTION = 0x0000B00C,
        OBJ_USE_IN_SCRIPT = 0x0000B013,
        SOUND = 0x0000CC01,
        SOUND_RANGE2 = 0x0000CC0B,
        PARTICL = 0x0000DD01,
        DIR_TYPE = 0x0000E005,
        MAIN_RANGE = 0x0000FF01,
        VSS_BS_COMMANDS = 0x00001E10,
        LIGHT_RANGE = 0x0000AA02,
        WS_AMBIENT = 0x0000ABD4,
        OBJTYPE = 0x0000B003,
        OBJBODYPARTS = 0x0000B00D,
        OBJ_IS_SHADOW = 0x0000B014,
        SOUND_ID = 0x0000CC02,
        PARTICL_ID = 0x0000DD02,
        RANGE = 0x0000FF02,
        VSS_SECTION = 0x00001E00,
        VSS_ISSTART = 0x00001E0A,
        VSS_CUSTOM_SRIPT = 0x00001E11,
        LIGHT_NAME = 0x0000AA03,
        WS_SUN_LIGHT = 0x0000ABD5,
        OBJNAME = 0x0000B004,
        PARENTTEMPLATE = 0x0000B00E,
        OBJ_R = 0x0000B015,
        SOUND_POSITION = 0x0000CC03,
        SOUND_AMBIENT = 0x0000CC0D,
        PARTICL_POSITION = 0x0000DD03,
        UNIT = 0xBBBB0000,
        UNIT_NEED_IMPORT = 0xBBBB000A,
        VSS_TRIGER = 0x00001E01,
        VSS_LINK = 0x00001E0B,
        LIGHT_POSITION = 0x0000AA04,
        OBJINDEX = 0x0000B005,
        OBJCOMMENTS = 0x0000B00F,
        OBJ_QUEST_INFO = 0x0000B016,
        SOUND_RANGE = 0x0000CC04,
        SOUND_IS_MUSIC = 0x0000CC0E,
        PARTICL_COMMENTS = 0x0000DD04,
        MAGIC_TRAP = 0xBBAB0000,
        UNIT_R = 0xBBBB0001,
        UNIT_LOGIC = 0xBBBC0000,
        UNIT_LOGIC_WAIT = 0xBBBC000A,
        VSS_CHECK = 0x00001E02,
        VSS_GROUP = 0x00001E0C,
        LIGHT_ID = 0x0000AA05,
        OBJTEMPLATE = 0x0000B006,
        SOUND_NAME = 0x0000CC05,
        PARTICL_NAME = 0x0000DD05,
        MIN_ID = 0x0000FF05,
        MT_DIPLOMACY = 0xBBAB0001,
        LEVER = 0xBBAC0000,
        UNIT_PROTOTYPE = 0xBBBB0002,
        UNIT_LOGIC_AGRESSIV = 0xBBBC0001,
        UNIT_LOGIC_ALARM_CONDITION = 0xBBBC000B,
        GUARD_PT = 0xBBBD0000,
        VSS_PATH = 0x00001E03,
        VSS_IS_USE_GROUP = 0x00001E0D,
        LIGHT_SHADOW = 0x0000AA06,
        OBJPRIMTXTR = 0x0000B007,
        SOUND_MIN = 0x0000CC06,
        PARTICL_TYPE = 0x0000DD06,
        MAX_ID = 0x0000FF06,
        MT_SPELL = 0xBBAB0002,
        LEVER_SCIENCE_STATS = 0xBBAC0001,
        UNIT_ITEMS = 0xBBBB0003,
        UNIT_LOGIC_CYCLIC = 0xBBBC0002,
        UNIT_LOGIC_HELP = 0xBBBC000C,
        GUARD_PT_POSITION = 0xBBBD0001,
        ACTION_PT = 0xBBBE0000,
        VSS_ID = 0x00001E04,
        VSS_VARIABLE = 0x00001E0E,
        LIGHT_COLOR = 0x0000AA07,
        OBJSECTXTR = 0x0000B008,
        SOUND_MAX = 0x0000CC07,
        PARTICL_SCALE = 0x0000DD07,
        AIGRAPH = 0x31415926,
        MT_AREAS = 0xBBAB0003,
        LEVER_CUR_STATE = 0xBBAC0002,
        UNIT_STATS = 0xBBBB0004,
        UNIT_LOGIC_MODEL = 0xBBBC0003,
        UNIT_LOGIC_ALWAYS_ACTIVE = 0xBBBC000D,
        GUARD_PT_ACTION = 0xBBBD0002,
        ACTION_PT_LOOK_PT = 0xBBBE0001,
        TORCH = 0xBBBF0000,
        VSS_RECT = 0x00001E05,
        VSS_BS_CHECK = 0x00001E0F,
        LIGHT_COMMENTS = 0x0000AA08,
        OBJPOSITION = 0x0000B009,
        SOUND_COMMENTS = 0x0000CC08,
        MT_TARGETS = 0xBBAB0004,
        LEVER_TOTAL_STATE = 0xBBAC0003,
        UNIT_QUEST_ITEMS = 0xBBBB0005,
        UNIT_LOGIC_GUARD_R = 0xBBBC0004,
        UNIT_LOGIC_AGRESSION_MODE = 0xBBBC000E,
        ACTION_PT_WAIT_SEG = 0xBBBE0002,
        TORCH_STRENGHT = 0xBBBF0001,
        VSS_SRC_ID = 0x00001E06,
        SOUND_VOLUME = 0x0000CC09,
        MT_CAST_INTERVAL = 0xBBAB0005,
        LEVER_IS_CYCLED = 0xBBAC0004,
        UNIT_QUICK_ITEMS = 0xBBBB0006,
        UNIT_LOGIC_GUARD_PT = 0xBBBC0005,
        ACTION_PT_TURN_SPEED = 0xBBBE0003,
        TORCH_PTLINK = 0xBBBF0002,
        VSS_DST_ID = 0x00001E07,
        LEVER_CAST_ONCE = 0xBBAC0005,
        UNIT_SPELLS = 0xBBBB0007,
        UNIT_LOGIC_NALARM = 0xBBBC0006,
        ACTION_PT_FLAGS = 0xBBBE0004,
        TORCH_SOUND = 0xBBBF0003,
        VSS_TITLE = 0x00001E08,
        LEVER_SCIENCE_STATS_NEW = 0xBBAC0006,
        UNIT_WEAPONS = 0xBBBB0008,
        UNIT_LOGIC_USE = 0xBBBC0007,
        VSS_COMMANDS = 0x00001E09,
        DIRICTORY_ELEMENTS = 0x0000F000,
        LEVER_IS_DOOR = 0xBBAC0007,
        UNIT_ARMORS = 0xBBBB0009,
        UNIT_LOGIC_REVENGE = 0xBBBC0008,
        DIRICTORY = 0x0000E000,
        SS_TEXT_OLD = 0xACCEECCA,
        LEVER_RECALC_GRAPH = 0xBBAC0008,
        UNIT_LOGIC_FEAR = 0xBBBC0009,
        SC_OBJECTDBFILE = 0x0000C000,
        FOLDER = 0x0000E001,
        SS_TEXT = 0xACCEECCB
    }
}
