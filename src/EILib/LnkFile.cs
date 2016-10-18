using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EILib
{
    public class LnkFile
    {
        public List<LnkRecord> Records { get; private set; }

        public LnkFile()
        {
            Records = new List<LnkRecord>();
        }

        public void Load(string path)
        {
            using (var fs = File.Open(path, FileMode.Open))
            {
                BinaryReader reader = new BinaryReader(fs);
                Records.Clear();
                uint linksCount = reader.ReadUInt32();
                for (int i = 0; i < linksCount; i++)
                {
                    var link = new LnkRecord();
                    link.Load(reader);
                    Records.Add(link);
                }
            }
        }

        public void Save(string path)
        {
            using (var fs = File.Open(path, FileMode.Create))
            {
                BinaryWriter writer = new BinaryWriter(fs);
                writer.Write((uint)Records.Count);
                for (int i = 0; i < Records.Count; i++)
                {
                    LnkRecord link = Records[i];
                    link.Save(writer);
                }
            }
        }
    }

    public class LnkRecord
    {
        public string Child;
        public string Parent;

        public void Load(BinaryReader reader)
        {
            var encoding = Encoding.GetEncoding(1251);
            Child = encoding.GetString(reader.ReadBytes(reader.ReadInt32()));
            Parent = encoding.GetString(reader.ReadBytes(reader.ReadInt32()));
            if (Child.Length > 0 && Child[Child.Length - 1] == '\0')
                Child = Child.Substring(0, Child.Length - 1);
            if (Parent.Length > 0 && Parent[Parent.Length - 1] == '\0')
                Parent = Parent.Substring(0, Parent.Length - 1);
        }

        public void Save(BinaryWriter writer)
        {
            var encoding = Encoding.GetEncoding(1251);
            if (Child.Length > 0)
            {
                writer.Write((int)Child.Length + 1);
                writer.Write(encoding.GetBytes(Child));
                writer.Write((byte)0);
            }
            else
                writer.Write((int)0);

            if (Parent.Length > 0)
            {
                writer.Write((int)Parent.Length + 1);
                writer.Write(encoding.GetBytes(Parent));
                writer.Write((byte)0);
            }
            else
                writer.Write((int)0);
        }
    }
}
