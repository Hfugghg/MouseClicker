using Xunit;

namespace MouseClicker.Tests
{
    public class UpdateVersionTests
    {
        [Theory]
        [InlineData("v1.2.3", "1.2.3.0")]
        [InlineData("V1.2.3", "1.2.3.0")]
        [InlineData("1.2.3", "1.2.3.0")]
        [InlineData("1.2.3.4", "1.2.3.4")]
        [InlineData("v0.0.0", "0.0.0.0")]
        public void StableTags_AreNormalizedToFourComponents(string value, string expected)
        {
            Assert.True(UpdateVersion.TryParseStable(value, out Version? parsed));
            Assert.Equal(new Version(expected), parsed);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("v")]
        [InlineData("1.2")]
        [InlineData("1.2.3.4.5")]
        [InlineData("1.2.3-beta")]
        [InlineData("1.2.3+build")]
        [InlineData(" 1.2.3")]
        [InlineData("1.2.3 ")]
        [InlineData("01.2.3")]
        [InlineData("1.-2.3")]
        [InlineData("1..3")]
        [InlineData("1.2.99999999999999")]
        public void NonStableOrMalformedTags_AreRejected(string? value)
        {
            Assert.False(UpdateVersion.TryParseStable(value, out Version? parsed));
            Assert.Null(parsed);
        }

        [Fact]
        public void ExistingAssemblyAndTagVersions_CompareEqually()
        {
            Assert.Equal(UpdateVersion.Normalize(new Version(1, 2, 1)), UpdateVersion.Normalize(new Version(1, 2, 1, 0)));
            Assert.Equal(new Version(1, 2, 0, 0), UpdateVersion.Normalize(new Version(1, 2)));
        }
    }
}
