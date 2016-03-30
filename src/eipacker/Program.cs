using System;
using System.IO;

namespace EIPacker
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: eipacker.exe path [output_path]\n" +
                    "Small tool for packing/unpacking/converting \"Evil Islands\" game files\n\n" +
                    "Features:\n" +
                    "1) Packs directory to .res archive\n" +
                    "2) Unpacks .res archive to directory\n" +
                    "3) Converts .reg file to .ini file (not implemented yet)\n" +
                    "4) Converts .ini file to .reg file (not implemented yet)\n" +
                    "5) Converts .lnk file to .txt file (not implemented yet)\n" +
                    "6) Converts .txt file to .lnk file (not implemented yet)\n");

                return 0;
            }

            ExitCode exitCode = 0;
            string path = Path.GetFullPath(args[0]);
            string outputPath = args.Length > 1 ? args[1] : null;
            switch (GetTargetType(path))
            {
                case TargetType.Nonexistent:
                    Console.Error.WriteLine("File " + path + " is not exists");
                    exitCode = ExitCode.FileNotFound;
                    break;
                case TargetType.Directory:
                    exitCode = PackDirectory(path, outputPath);
                    break;
                case TargetType.ResFile:
                    exitCode = UnpackArchive(path, outputPath);
                    break;
                case TargetType.IniFile:
                case TargetType.RegFile:
                case TargetType.TxtFile:
                case TargetType.LnkFile:
                    Console.Error.WriteLine("This file format are not yet supported: " + path);
                    exitCode = ExitCode.FormatNotSupported;
                    break;
                case TargetType.Unknown:
                default:
                    Console.Error.WriteLine("Unknown file format: " + path);
                    exitCode = ExitCode.Unknown;
                    break;
            }

            return (int)exitCode;
        }

        private static ExitCode PackDirectory(string path, string outputPath)
        {
            try
            {
                if (outputPath == null)
                {
                    outputPath = path + Path.DirectorySeparatorChar + ".." +
                        Path.DirectorySeparatorChar + GetPackedName(path);
                }

                EILib.ResFileHelper.CreateFromDirectory(path, outputPath);
                Console.Error.WriteLine("Directory " + path + " successfully packed");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Cannot pack directory " + path +
                    "\n" + e.Message + "\n" + e.StackTrace);
                return ExitCode.Unknown;
            }

            return ExitCode.Success;
        }

        private static ExitCode UnpackArchive(string path, string outputPath)
        {
            try
            {
                if (outputPath == null)
                {
                    outputPath = Path.GetDirectoryName(path) + Path.DirectorySeparatorChar + GetUnpackedName(path);
                }

                EILib.ResFileHelper.ExtractToDirectory(path, outputPath);
                Console.Error.WriteLine("File " + path + " successfully unpacked");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Cannot unpack file " + path +
                    "\n" + e.Message + "\n" + e.StackTrace);
                return ExitCode.Unknown;
            }

            return ExitCode.Success;
        }

        private static TargetType GetTargetType(string fullPath)
        {
            if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
                return TargetType.Nonexistent;

            if (File.GetAttributes(fullPath) == FileAttributes.Directory)
                return TargetType.Directory;

            // Detect by extension
            if (fullPath.EndsWith(".ini", StringComparison.OrdinalIgnoreCase))
                return TargetType.IniFile;
            if (fullPath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                return TargetType.TxtFile;
            if (fullPath.EndsWith(".reg", StringComparison.OrdinalIgnoreCase))
                return TargetType.RegFile;

            // Detect by signature
            uint signature;
            using (var f = File.OpenRead(fullPath))
                signature = new BinaryReader(f).ReadUInt32();

            if (signature == 0x019CE23C) // res signature
                return TargetType.ResFile;
            if (signature == 0x45ab3efb) // reg signature
                return TargetType.RegFile;

            return TargetType.Unknown;
        }

        private static string GetPackedName(string path)
        {
            var parts = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar)).Split('_');
            string result;
            if (parts.Length > 1)
                result = string.Join("_", parts, 0, parts.Length - 1) + "." + parts[parts.Length - 1];
            else
                result = parts[0] + "_";

            return result;
        }

        private static string GetUnpackedName(string path)
        {
            var parts = Path.GetFileName(path).Split('.');
            string result;
            if (parts.Length > 1)
                result = string.Join(".", parts, 0, parts.Length - 1) + "_" + parts[parts.Length - 1];
            else
                result = parts[0] + "_";

            return result;
        }

        private enum TargetType
        {
            Unknown,
            Nonexistent,
            Directory,
            ResFile,
            RegFile,
            IniFile,
            LnkFile,
            TxtFile
        }

        private enum ExitCode : int
        {
            Unknown = -1,
            Success = 0,
            FileNotFound = 1,
            FormatNotSupported = 2,
            FormatUnknown = 3
        }
    }
}
