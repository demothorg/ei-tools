using System.IO;

namespace EILib
{
    public class ResFileHelper
    {
        /// <summary>
        /// Creates a Res archive at the path destinationArchiveFileName that contains the files and directories from
        /// the directory specified by sourceDirectoryName. The directory structure is preserved in the archive, and a
        /// recursive search is done for files to be archived. If the directory is empty, an empty
        /// archive will be created. If a file in the directory cannot be added to the archive, the archive will be left incomplete
        /// and invalid and the method will throw an exception. This method does not include the base directory into the archive.
        /// If an error is encountered while adding files to the archive, this method will stop adding files and leave the archive
        /// in an invalid state. The paths are permitted to specify relative or absolute path information. Relative path information
        /// is interpreted as relative to the current working directory.
        /// </summary>
        ///
        /// <param name="sourceDirectoryName">The path to the directory on the file system to be archived.</param>
        /// <param name="destinationArchiveFileName">The name of the archive to be created.</param>
        public static void CreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName)
        {
            sourceDirectoryName = Path.GetFullPath(sourceDirectoryName) + Path.DirectorySeparatorChar;

            using (var f = new FileStream(destinationArchiveFileName, FileMode.Create))
            {
                using (var res = new ResFile(f))
                {
                    var files = Directory.GetFiles(sourceDirectoryName, "*.*", SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        var data = File.ReadAllBytes(file);
                        res.AddFile(Utility.GetRelativePath(sourceDirectoryName, file), File.GetLastWriteTimeUtc(file));
                        f.Write(data, 0, data.Length);
                    }
                }
            }
        }

        /// <summary>
        /// Extracts all of the files in the specified archive to a directory on the file system.
        /// This method will create all subdirectories and the specified directory.
        /// If there is an error while extracting the archive, the archive will remain partially extracted. Each entry will
        /// be extracted such that the extracted file has the same relative path to the destinationDirectoryName as the entry
        /// has to the archive. The path is permitted to specify relative or absolute path information. Relative path information
        /// is interpreted as relative to the current working directory.
        /// </summary>
        ///
        /// <param name="sourceArchiveFileName">The path to the archive on the file system that is to be extracted.</param>
        /// <param name="destinationDirectoryName">The path to the directory on the file system.</param>
        public static void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName)
        {
            sourceArchiveFileName = Path.GetFullPath(sourceArchiveFileName);

            using (var f = new FileStream(sourceArchiveFileName, FileMode.Open))
            {
                var reader = new BinaryReader(f);
                var files = ResFile.GetFiles(f);
                foreach (var file in files.Values)
                {
                    string fileName = destinationDirectoryName + Path.DirectorySeparatorChar + file.FileName;
                    string dirName = Path.GetDirectoryName(fileName);
                    Directory.CreateDirectory(dirName);

                    f.Seek(file.Position, SeekOrigin.Begin);
                    File.WriteAllBytes(fileName, reader.ReadBytes(file.Size));
                    File.SetLastWriteTimeUtc(fileName, file.LastWriteTime);
                    File.SetLastAccessTimeUtc(fileName, file.LastWriteTime);
                }
            }
        }
    }
}
