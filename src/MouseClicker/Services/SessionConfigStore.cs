using System.IO;
using System.Text.Json;

namespace MouseClicker;

internal sealed class SessionConfigStore
{
    private readonly string _filePath;

    internal SessionConfigStore(string? filePath = null)
    {
        _filePath = Path.GetFullPath(filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MouseClicker", "last-session.json"));
    }

    internal ConfigData? Load() => File.Exists(_filePath)
        ? JsonSerializer.Deserialize<ConfigData>(File.ReadAllText(_filePath))
        : null;

    internal void BackupUnreadableConfig()
    {
        File.Copy(_filePath, _filePath + "." + Guid.NewGuid().ToString("N") + ".bak");
    }

    internal void Save(ConfigData config)
    {
        string directory = Path.GetDirectoryName(_filePath)!;
        Directory.CreateDirectory(directory);
        string temporary = Path.Combine(directory, Path.GetFileName(_filePath) + "." + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(config));
            File.Move(temporary, _filePath, overwrite: true);
        }
        finally
        {
            try { File.Delete(temporary); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        }
    }
}
