using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Media.Imaging;

namespace Wallppr;

public sealed class WallpaperThumbnailCache(string? directory = null)
{
    private const int PreviewWidth = 640;
    private readonly SemaphoreSlim generationSlots = new(2);
    private readonly string directory = directory ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "wallppr",
        "thumbnails");

    public string GetPath(string displayId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayId);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(displayId)));
        return Path.Combine(directory, $"{hash}.jpg");
    }

    public string? GetExistingPath(string displayId)
    {
        var path = GetPath(displayId);
        return File.Exists(path) ? path : null;
    }

    public async Task<string?> CreateAsync(string displayId, string sourcePath, CancellationToken cancellationToken = default)
    {
        var path = GetPath(displayId);
        var temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";
        await generationSlots.WaitAsync(cancellationToken);
        try
        {
            await Task.Run(() =>
            {
                Directory.CreateDirectory(directory);
                var bitmap = Load(sourcePath, PreviewWidth);
                var encoder = new JpegBitmapEncoder { QualityLevel = 85 };
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using var output = File.Create(temporaryPath);
                encoder.Save(output);
                output.Close();
                File.Move(temporaryPath, path, overwrite: true);
            }, cancellationToken);
            return path;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return null;
        }
        finally
        {
            generationSlots.Release();
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    public async Task<BitmapSource?> LoadAsync(string? path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return null;
        }

        try
        {
            return await Task.Run<BitmapSource?>(() => Load(path, decodePixelWidth: 0), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            try
            {
                File.Delete(path);
            }
            catch
            {
            }
            return null;
        }
    }

    private static BitmapImage Load(string path, int decodePixelWidth)
    {
        using var input = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        if (decodePixelWidth > 0)
        {
            bitmap.DecodePixelWidth = decodePixelWidth;
        }
        bitmap.StreamSource = input;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }
}
