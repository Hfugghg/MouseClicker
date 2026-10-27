using System.IO;
using System.Text;
using System.Text.Json;

namespace MouseClicker
{
    internal sealed class UpdatePreferencesStore
    {
        private readonly string _filePath;
        private readonly object _gate = new();
        private HashSet<string> _skippedVersions;

        public UpdatePreferencesStore(string? filePath = null)
        {
            _filePath = Path.GetFullPath(filePath ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MouseClicker", "update-preferences.json"));
            _skippedVersions = Load();
        }

        public bool IsSkipped(Version version)
        {
            string key = UpdateVersion.Normalize(version).ToString();
            lock (_gate) return _skippedVersions.Contains(key);
        }

        public void Skip(Version version)
        {
            string key = UpdateVersion.Normalize(version).ToString();
            lock (_gate)
            {
                if (_skippedVersions.Contains(key)) return;
                var updated = new HashSet<string>(_skippedVersions, StringComparer.Ordinal) { key };
                string directory = Path.GetDirectoryName(_filePath)!;
                Directory.CreateDirectory(directory);
                string temporary = Path.Combine(directory, Path.GetFileName(_filePath) + "." + Guid.NewGuid().ToString("N") + ".tmp");
                try
                {
                    var preferences = new StoredPreferences { SkippedVersions = updated.OrderBy(item => item, StringComparer.Ordinal).ToArray() };
                    File.WriteAllText(temporary, JsonSerializer.Serialize(preferences), new UTF8Encoding(false));
#if NET48
                    if (File.Exists(_filePath)) File.Delete(_filePath);
                    File.Move(temporary, _filePath);
#else
                    File.Move(temporary, _filePath, overwrite: true);
#endif
                    // 保存失败时，本次会话余下的时间内不得隐藏该更新。
                    _skippedVersions = updated;
                }
                finally
                {
                    try { File.Delete(temporary); }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
                }
            }
        }

        private HashSet<string> Load()
        {
            var skipped = new HashSet<string>(StringComparer.Ordinal);
            try
            {
                StoredPreferences? preferences = JsonSerializer.Deserialize<StoredPreferences>(File.ReadAllText(_filePath));
                foreach (string? value in preferences?.SkippedVersions ?? Array.Empty<string>())
                {
                    if (UpdateVersion.TryParseStable(value, out Version? version)) skipped.Add(version.ToString());
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                // 偏好设置缺失、无法读取或损坏时，不应阻止程序启动。
            }
            return skipped;
        }

        private sealed class StoredPreferences
        {
            public string[]? SkippedVersions { get; set; }
        }
    }
}
