using System;
using System.Collections.Generic;
using Xunit;

namespace EILib.Tests
{
    public class GetEIStringHashTests
    {
        [Fact]
        public void Hash32WithNullStringShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("value", () => Utility.GetEIStringHash32(null, 0));
        }

        [Fact]
        public void Hash16WithNullStringShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("value", () => Utility.GetEIStringHash16(null, 0));
        }

        [Theory]
        [InlineData(2029, @"C:\Temp\SomeFile.txt", 0)]
        [InlineData(53, @"C:\Temp\SomeFile.txt", 152)]
        [InlineData(1488, "x\u0430\u0410\u044f\u042f\u044d\u042d", 0)]
        [MemberData("LongString32Data")]
        public void Hash32WithNormalValueShouldCalculateCorrectChecksum(uint expectedValue, string hashedString, uint hashTableSize)
        {
            Assert.Equal(expectedValue, Utility.GetEIStringHash32(hashedString, hashTableSize));
        }

        [Theory]
        [InlineData(2029, @"C:\Temp\SomeFile.txt", 0)]
        [InlineData(53, @"C:\Temp\SomeFile.txt", 152)]
        [InlineData(1488, "x\u0430\u0410\u044f\u042f\u044d\u042d", 0)]
        [MemberData("LongString16Data")]
        public void Hash16WithNormalValueShouldCalculateCorrectChecksum(ushort expectedValue, string hashedString, ushort hashTableSize)
        {
            Assert.Equal(expectedValue, Utility.GetEIStringHash16(hashedString, hashTableSize));
        }

        public static IEnumerable<object[]> LongString32Data
        {
            get
            {
                return new[]
                {
                    new object[] { 81130, new string('z', 665),  0},
                    new object[] { 31130, new string('z', 665),  50000}
                };
            }
        }

        public static IEnumerable<object[]> LongString16Data
        {
            get
            {
                return new[]
                {
                    new object[] { 15594, new string('z', 665),  0},
                    new object[] { 31130, new string('z', 665),  50000}
                };
            }
        }
    }
}
