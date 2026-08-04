using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wallppr;

public sealed class JsonSettingsStore(string? path = null) : ISettingsStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public string Path { get; } = path ?? System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "wallppr",
        "settings.json");

    public WallpprSettings Load()
    {
        if (!File.Exists(Path))
        {
            return new WallpprSettings();
        }

        return JsonSerializer.Deserialize<WallpprSettings>(File.ReadAllText(Path), Options)
            ?? new WallpprSettings();
    }

    public void Save(WallpprSettings settings)
    {
        var directory = System.IO.Path.GetDirectoryName(Path)!;
        Directory.CreateDirectory(directory);
        var temporaryPath = Path + ".tmp";

        try
        {
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings, Options));
            File.Move(temporaryPath, Path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }
}
