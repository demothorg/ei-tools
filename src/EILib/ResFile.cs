using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EILib
{
    public class ResFile : IDisposable
    {
        private readonly long streamStartPosition;
        private readonly Stream stream;
        private readonly List<ResFileEntry> entries;
        private bool isDisposed;

        public ResFile(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            if (!stream.CanSeek || !stream.CanWrite)
                throw new ArgumentException("Stream must be seekable and writeable", "stream");

            this.isDisposed = false;
            this.stream = stream;
            this.streamStartPosition = stream.Position;
            this.entries = new List<ResFileEntry>();

            var header = new ResFileHeader()
            {
                Signature = ResFileHeader.ResFileSignature,
                TableSize = 0,
                TableOffset = 0,
                NamesLength = 0
            };

            Utility.WriteBytes(this.stream, Utility.GetBytes(header));
        }

        public static Dictionary<string, ResFileEntry> GetFiles(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            if (!stream.CanSeek || !stream.CanRead)
                throw new ArgumentException("Stream must be seekable and readable", "stream");

            var streamStartPosition = stream.Position;
            var header = ReadResHeader(stream);
            if (header.Signature != ResFileHeader.ResFileSignature)
                throw new InvalidDataException();

            stream.Seek(streamStartPosition + header.TableOffset, SeekOrigin.Begin);
            var fileTable = ReadResFileHashTable(stream, header.TableSize);
            var names = ReadNamesBuffer(stream, header.NamesLength);

            var result = BuildFileTableDictionary(fileTable, streamStartPosition,
                stream.Length, Encoding.GetEncoding(1251).GetString(names));

            return result;
        }

        public void AddFile(string name, DateTime time)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            if (Utility.FilePathHasInvalidChars(name))
                throw new ArgumentException("Path contains invalid charaters", "name");

            if (Encoding.GetEncoding(1251).GetByteCount(name) > ushort.MaxValue)
                throw new ArgumentOutOfRangeException("name");

            if (this.isDisposed)
                throw new ObjectDisposedException(this.GetType().ToString());

            name = name.Replace('/', '\\');
            this.CompletePreviousFile();

            var entry = new ResFileEntry()
            {
                FileName = name,
                Position = this.stream.Position,
                Size = 0,
                LastWriteTime = time
            };

            this.entries.Add(entry);
        }

        public void Dispose()
        {
            if (!this.isDisposed)
            {
                try
                {
                    this.Complete();
                }
                finally
                {
                    // this.stream.Dispose();
                    this.isDisposed = true;
                }
            }
        }

        private static Dictionary<string, ResFileEntry> BuildFileTableDictionary(
            ResFileHashTableEntry[] fileTable, long streamOffset, long streamLength, string names)
        {
            var result = new Dictionary<string, ResFileEntry>(new Utility.AsciiIgnoreCaseComparer());
            foreach (var file in fileTable)
            {
                if (file.DataOffset + file.DataSize > streamLength - streamOffset)
                    throw new InvalidDataException();

                string name = names.Substring((int)file.NameOffset, file.NameLength);
                var entry = new ResFileEntry();
                entry.FileName = name;
                entry.LastWriteTime = Utility.ConvertFromUnixTimestamp(file.LastWriteTime);
                entry.Position = streamOffset + file.DataOffset;
                entry.Size = (int)file.DataSize;

                result.Add(name, entry);
            }

            return result;
        }

        private static ResFileHeader ReadResHeader(Stream stream)
        {
            return Utility.ReadStructure<ResFileHeader>(stream);
        }

        private static ResFileHashTableEntry[] ReadResFileHashTable(Stream stream, uint size)
        {
            var hashTable = new ResFileHashTableEntry[size];
            for (long i = 0; i < size; i++)
                hashTable[i] = Utility.ReadStructure<ResFileHashTableEntry>(stream);

            return hashTable;
        }

        private static byte[] ReadNamesBuffer(Stream stream, uint size)
        {
            var bytes = new BinaryReader(stream).ReadBytes((int)size);
            if (bytes.Length != size)
                throw new InvalidDataException();

            return bytes;
        }

        private static void BuildResHashTable(
            ICollection<ResFileEntry> entries, long headerPosition,
            out ResFileHashTableEntry[] hashTable, out byte[] namesBuffer)
        {
            uint hashTableSize = (uint)entries.Count;
            var names = new List<byte>();
            hashTable = new ResFileHashTableEntry[hashTableSize];
            for (int i = 0; i < hashTable.Length; i++)
            {
                hashTable[i].NextIndex = uint.MaxValue;
                hashTable[i].DataOffset = 0;
            }

            uint lastFreeIndex = (uint)hashTable.Length - 1;
            foreach (var entry in entries)
            {
                uint index = Utility.GetEIStringHash32(entry.FileName, hashTableSize);
                if (hashTable[index].DataOffset != 0)
                {
                    while (hashTable[index].NextIndex != uint.MaxValue)
                        index = hashTable[index].NextIndex;

                    while (hashTable[lastFreeIndex].DataOffset != 0)
                        lastFreeIndex--;

                    hashTable[index].NextIndex = lastFreeIndex;
                    index = lastFreeIndex;
                    lastFreeIndex--;
                }

                hashTable[index].LastWriteTime = (uint)Utility.ConvertToUnixTimestamp(entry.LastWriteTime);
                hashTable[index].DataOffset    = (uint)(entry.Position - headerPosition);
                hashTable[index].DataSize      = (uint)entry.Size;
                hashTable[index].NameOffset    = (uint)names.Count;
                hashTable[index].NameLength    = AppendNamesBuffer(names, entry.FileName);
                hashTable[index].NextIndex = uint.MaxValue;
            }

            namesBuffer = names.ToArray();
        }

        private static ushort AppendNamesBuffer(List<byte> buffer, string name)
        {
            var bytes = Encoding.GetEncoding(1251).GetBytes(name);
            buffer.AddRange(bytes);
            if (bytes.Length > ushort.MaxValue)
                throw new ArgumentOutOfRangeException("name");

            return (ushort)bytes.Length;
        }

        private void Complete()
        {
            if (this.entries.Count == 0)
                return;

            this.CompletePreviousFile();

            var stream = this.stream;
            var headerPos = this.streamStartPosition;
            var hashTablePos = stream.Position;

            // Build hash table
            ResFileHashTableEntry[] hashTable;
            byte[] namesBuffer;
            BuildResHashTable(this.entries, headerPos, out hashTable, out namesBuffer);

            // Write hash table
            for (int i = 0; i < hashTable.Length; i++)
                Utility.WriteBytes(stream, Utility.GetBytes(hashTable[i]));

            // Write names buffer
            Utility.WriteBytes(stream, namesBuffer);

            // Update header
            var header = new ResFileHeader()
            {
                Signature = ResFileHeader.ResFileSignature,
                TableSize = (uint)hashTable.Length,
                TableOffset = (uint)(hashTablePos - headerPos),
                NamesLength = (uint)namesBuffer.Length
            };

            var position = stream.Position;
            stream.Seek(this.streamStartPosition, SeekOrigin.Begin);
            Utility.WriteBytes(stream, Utility.GetBytes(header));
            this.stream.Seek(position, SeekOrigin.Begin);
        }

        private void CompletePreviousFile()
        {
            if (this.entries.Count == 0)
                return;

            var entry = this.entries[this.entries.Count - 1];
            entry.Size = (int)(this.stream.Position - entry.Position);
            this.entries[this.entries.Count - 1] = entry;

            this.WriteAlign();
        }

        private void WriteAlign()
        {
            long offset = this.stream.Position - this.streamStartPosition;
            this.stream.Seek(this.stream.Position + ((16 - (offset % 16)) % 16), SeekOrigin.Begin);
        }

        public struct ResFileEntry
        {
            public string   FileName;
            public long     Position;
            public int      Size;
            public DateTime LastWriteTime;
        }
    }
}
