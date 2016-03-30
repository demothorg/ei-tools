using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace EILib
{
    internal static class Utility
    {
        public static byte[] GetBytes<T>(T str) where T : struct
        {
            int size = Marshal.SizeOf(str);
            byte[] arr = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(str, ptr, true);
                Marshal.Copy(ptr, arr, 0, size);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }

            return arr;
        }

        public static T ReadStructure<T>(Stream stream) where T : struct
        {
            var size = Marshal.SizeOf(typeof(T));
            var buffer = new byte[size];
            if (stream.Read(buffer, 0, size) != size)
                throw new InvalidDataException();

            var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                return (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            }
            finally
            {
                handle.Free();
            }
        }

        public static DateTime ConvertFromUnixTimestamp(double timestamp)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return origin.AddSeconds(timestamp);
        }

        public static double ConvertToUnixTimestamp(DateTime date)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            TimeSpan diff = date - origin;
            return Math.Floor(diff.TotalSeconds);
        }

        public static bool FilePathHasInvalidChars(string path)
        {
            return !string.IsNullOrEmpty(path) && path.IndexOfAny(Path.GetInvalidPathChars()) >= 0;
        }

        public static void WriteBytes(Stream stream, byte[] data)
        {
            stream.Write(data, 0, data.Length);
        }

        public static string GetRelativePath(string rootPath, string path)
        {
            if (rootPath == null)
                throw new ArgumentNullException("rootPath");

            if (rootPath.Length == 0)
                throw new ArgumentOutOfRangeException("rootPath", "Root path cannot be empty");

            if (path == null)
                throw new ArgumentNullException("path");

            if (path.Length == 0)
                throw new ArgumentOutOfRangeException("path", "Path cannot be empty");

            var pathNormalized = Path.GetFullPath(path);
            var rootPathNormalized = Path.GetFullPath(rootPath).TrimEnd('\\', '/');

            if (!pathNormalized.StartsWith(rootPathNormalized))
                throw new ArgumentException();

            var res = pathNormalized.Substring(rootPathNormalized.Length + 1);
            return res;
        }

        public static char ToLowerAscii(char c)
        {
            if (c >= 'A' && c <= 'Z')
                return (char)(c + ('a' - 'A'));

            return c;
        }

        public static string ToLowerAscii(string s)
        {
            if (s == null)
                throw new ArgumentNullException("s");

            string result = string.Empty;
            for (int i = 0; i < s.Length; i++)
                result += ToLowerAscii(s[i]);

            return result;
        }

        public static uint GetEIStringHash32(string value, uint hashTableSize = 0)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            uint hash = 0;
            var valueAsBytes = Encoding.GetEncoding(1251).GetBytes(ToLowerAscii(value));
            foreach (var character in valueAsBytes)
                hash += character;

            return hashTableSize == 0
                ? hash
                : hash % hashTableSize;
        }

        public static ushort GetEIStringHash16(string value, ushort hashTableSize = 0)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            uint hash = GetEIStringHash32(value, 0);
            return hashTableSize == 0
                ? (ushort)hash
                : (ushort)(hash % hashTableSize);
        }

        public class AsciiIgnoreCaseComparer : IEqualityComparer<string>
        {
            public bool Equals(string x, string y)
            {
                if (x == null || y == null)
                    return x == y;

                if (x.Length != y.Length)
                    return false;

                for (int i = 0; i < x.Length; i++)
                {
                    if (ToLowerAscii(x[i]) != ToLowerAscii(y[i]))
                        return false;
                }

                return true;
            }

            public int GetHashCode(string s)
            {
                if (s == null)
                    return 0;

                // From here: http://stackoverflow.com/a/263416
                unchecked
                {
                    int hash = (int)2166136261;
                    for (var i = 0; i < s.Length; i++)
                        hash = hash * 16777619 ^ ToLowerAscii(s[i]).GetHashCode();

                    return hash;
                }
            }
        }
    }
}
