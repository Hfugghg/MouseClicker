using System.IO;
using System.Text.Json;
using Xunit;

namespace MouseClicker.Tests
{
    public class UpdatePreferencesStoreTests : IDisposable
    {
        private readonly string _directory = Path.Combine(Path.GetTempPath(), "MouseClickerUpdateTests-" + Guid.NewGuid().ToString("N"));
        private string PreferencesPath => Path.Combine(_directory, "update-preferences.json");

        [Fact]
        public void MissingPreferences_DoNotSkipAnyVersion()
        {
            var store = new UpdatePreferencesStore(PreferencesPath);
            Assert.False(store.IsSkipped(new Version(1, 3, 0)));
            Assert.False(Directory.Exists(_directory));
        }

        [Fact]
        public void Skip_PersistsExactVersionAcrossInstancesAndLeavesNextVersionVisible()
        {
            var first = new UpdatePreferencesStore(PreferencesPath);
            first.Skip(new Version(1, 3, 0));
            Assert.True(first.IsSkipped(new Version(1, 3, 0, 0)));
            var second = new UpdatePreferencesStore(PreferencesPath);
            Assert.True(second.IsSkipped(new Version(1, 3, 0)));
            Assert.False(second.IsSkipped(new Version(1, 3, 1)));
            Assert.False(second.IsSkipped(new Version(1, 4, 0)));
            second.Skip(new Version(1, 4, 0));
            var third = new UpdatePreferencesStore(PreferencesPath);
            Assert.True(third.IsSkipped(new Version(1, 3, 0)));
            Assert.True(third.IsSkipped(new Version(1, 4, 0)));
        }

        [Fact]
        public void EquivalentVersions_ArePersistedOnlyOnceWithoutTemporaryFiles()
        {
            var store = new UpdatePreferencesStore(PreferencesPath);
            store.Skip(new Version(1, 3, 0));
            store.Skip(new Version(1, 3, 0, 0));
            using var document = JsonDocument.Parse(File.ReadAllText(PreferencesPath));
            Assert.Equal("1.3.0.0", Assert.Single(document.RootElement.GetProperty("SkippedVersions").EnumerateArray()).GetString());
            Assert.Single(Directory.GetFiles(_directory));
        }

        [Theory]
        [InlineData("{")]
        [InlineData("null")]
        [InlineData("[]")]
        [InlineData("{\"SkippedVersions\":123}")]
        [InlineData("{\"SkippedVersions\":null}")]
        public void DamagedPreferences_AreSafeDefaults(string json)
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(PreferencesPath, json);
            Assert.False(new UpdatePreferencesStore(PreferencesPath).IsSkipped(new Version(1, 3, 0)));
        }

        [Fact]
        public void InvalidStoredVersionEntries_AreIgnored()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(PreferencesPath, "{\"SkippedVersions\":[\"v1.3.0\",null,\"1.4.0-beta\",\"1.5.0.0\"]}");
            var store = new UpdatePreferencesStore(PreferencesPath);
            Assert.True(store.IsSkipped(new Version(1, 3, 0)));
            Assert.True(store.IsSkipped(new Version(1, 5, 0)));
            Assert.False(store.IsSkipped(new Version(1, 4, 0)));
        }

        [Fact]
        public void UnreadablePreferences_DoNotPreventStartup()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(PreferencesPath, "{}");
            using var locked = new FileStream(PreferencesPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            Assert.False(new UpdatePreferencesStore(PreferencesPath).IsSkipped(new Version(1, 3, 0)));
        }

        [Fact]
        public void FailedSave_ThrowsWithoutMarkingVersionSkippedOrLeavingTemporaryFiles()
        {
            Directory.CreateDirectory(PreferencesPath);
            var store = new UpdatePreferencesStore(PreferencesPath);
            Exception? failure = Record.Exception(() => store.Skip(new Version(1, 3, 0)));
            Assert.True(failure is IOException or UnauthorizedAccessException, $"Expected filesystem failure, got {failure}");
            Assert.False(store.IsSkipped(new Version(1, 3, 0)));
            Assert.Empty(Directory.GetFiles(_directory, "*.tmp"));
        }

        public void Dispose()
        {
            string root = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!Path.GetFullPath(_directory).StartsWith(root, StringComparison.OrdinalIgnoreCase) ||
                !Path.GetFileName(_directory).StartsWith("MouseClickerUpdateTests-", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Unexpected temporary test directory.");
            }
            if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
        }
    }
}
