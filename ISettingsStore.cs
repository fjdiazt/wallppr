namespace Wallppr;

public interface ISettingsStore
{
    WallpprSettings Load();
    void Save(WallpprSettings settings);
}
