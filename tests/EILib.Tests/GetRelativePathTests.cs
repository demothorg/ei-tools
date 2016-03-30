using System;
using System.IO;
using Xunit;

namespace EILib.Tests
{
    public class GetRelativePathTests
    {
        [Fact]
        public void GetRelativePathWithNullRootShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("rootPath", () => Utility.GetRelativePath(null, @"C:\1"));
        }
        
        [Fact]
        public void GetRelativePathWithEmptyRootShouldThrowArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>("rootPath", () => Utility.GetRelativePath(string.Empty, @"C:\1"));
        }
        
        [Fact]
        public void GetRelativePathWithNullPathShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("path", () => Utility.GetRelativePath(@"C:\", null));
        }

        [Fact]
        public void GetRelativePathWithEmptyPathShouldThrowArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>("path", () => Utility.GetRelativePath(@"C:\", string.Empty));
        }

        [Theory]
        [InlineData("somefile.txt", @"/tmp", @"/tmp/somefile.txt")]
        [InlineData("somefile.txt", @"/tmp/", @"/tmp/somefile.txt")]
        [InlineData("somefile.txt", @"./dir/", @"dir/somefile.txt")]
        [InlineData("somefile.txt", @"./dir/dir2/..", @"dir/somefile.txt")]
        [InlineData("dir2/somefile.txt", @"dir1", @"dir1/dir2/somefile.txt")]
        [InlineData("Some File.txt", @"/tmp/", @"/tmp/Some File.txt")]
        [InlineData(@"temp\somefile.txt", @"..", @"..\temp\somefile.txt")]
        [InlineData(@"temp\somefile.txt", @".", @".\temp\somefile.txt")]
        public void GetRelativePathWithNormalValueShouldReturnCorrectPath(string expectedPath, string rootPath, string filePath)
        {
            Assert.Equal(expectedPath.Replace('/', Path.DirectorySeparatorChar), Utility.GetRelativePath(rootPath, filePath));
        }
    }
}
