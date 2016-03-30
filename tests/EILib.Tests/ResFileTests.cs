using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Xunit;

namespace EILib.Tests
{
    public class ResFileTests
    {
        [Fact]
        public void ResFileShouldContainsExpectedSelfGeneratedFiles()
        {
            var test = GenerateResArchiveTest(655360001);
            var stream = (Stream)test[0];
            var expectedFiles = (FileInfo[])test[1];

            var files = ResFile.GetFiles(stream);
            Assert.Equal(files.Count, expectedFiles.Length);

            foreach (var expectedFile in expectedFiles)
            {
                string name = expectedFile.Name.Replace('/', '\\');
                ResFile.ResFileEntry file;
                Assert.True(files.TryGetValue(name, out file));

                Assert.Equal(file.FileName, name);
                Assert.Equal(file.LastWriteTime, expectedFile.Time);
                Assert.Equal(file.Size, expectedFile.Size);

                var data = new byte[file.Size];
                stream.Seek(file.Position, SeekOrigin.Begin);
                stream.Read(data, 0, data.Length);
                Assert.Equal(MD5.Create().ComputeHash(data), expectedFile.Hash);
            }
        }

        private static string GetRandomName(int maxLength, Random rnd)
        {
            string result = "";
            int length = rnd.Next(1, maxLength);
            char ch;
            for (int i = 0; i < length || result == "." || result == ".." || string.IsNullOrWhiteSpace(result); i++)
            {
                switch (rnd.Next(5))
                {
                    case 0:
                        ch = (char)(48 + rnd.Next(10)); // digit
                        break;
                    case 1:
                        ch = (char)(65 + rnd.Next(26)); // upper letter
                        break;
                    case 2:
                        ch = (char)(97 + rnd.Next(26)); // lower letter
                        break;
                    case 3:
                        ch = (char)(0x0410 + rnd.Next(64)); // cyrillic letter
                        break;
                    default:
                        const string symbols = @" !#$%&'()+,-.;=@[]^_`{}~";
                        ch = symbols[rnd.Next(symbols.Length)];
                        break;
                }

                result += ch;
            }

            return result;
        }

        public class FileInfo
        {
            public string Name;
            public DateTime Time;
            public int Size;
            public byte[] Hash;
        }

        private static List<FileInfo> GenerateFilesRecursive(ResFile res, Stream stream, Random rnd,
            int filesCount, int maxDeep = 0, string folderName = "")
        {
            var files = new List<FileInfo>();
            var names = new List<string>();
            while (filesCount > 0)
            {
                string name = GetRandomName(20, rnd);
                if (names.IndexOf(Utility.ToLowerAscii(name)) >= 0)
                    continue;

                names.Add(Utility.ToLowerAscii(name));
                name = folderName + name;

                if (maxDeep == 0 || rnd.Next(2) == 0)
                {
                    var file = new FileInfo()
                    {
                        Name = name,
                        Time = new DateTime(1980, 1, 1).AddSeconds(rnd.Next(int.MaxValue)),
                        Size = rnd.Next(5000)
                    };

                    var fileData = new byte[file.Size];
                    rnd.NextBytes(fileData);
                    file.Hash = MD5.Create().ComputeHash(fileData);

                    res.AddFile(file.Name, file.Time);
                    stream.Write(fileData, 0, fileData.Length);

                    files.Add(file);
                    filesCount--;
                }
                else
                {
                    int count = 1 + rnd.Next(filesCount);
                    files.AddRange(
                        GenerateFilesRecursive(res, stream, rnd, count, maxDeep - 1,
                        name + (rnd.Next(2) == 0 ? '/' : '\\')));
                    filesCount -= count;
                }
            }

            return files;
        }

        public static object[] GenerateResArchiveTest(int seed)
        {
            var stream = new MemoryStream();
            Random rnd = new Random(seed);
            List<FileInfo> files;
            using (var res = new ResFile(stream))
            {
                files = GenerateFilesRecursive(res, stream, rnd,
                    rnd.Next(5000, 10000), rnd.Next(10));
            }

            stream.Seek(0, SeekOrigin.Begin);
            return new object[]
            {
                stream,
                files.ToArray()
            };
        }
    }
}
