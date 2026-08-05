using System.Windows.Threading;

namespace Wallppr;

public sealed record SlideshowRunResult(
    IReadOnlyList<DisplayProfile> Changed,
    IReadOnlyList<string> Errors);
public static class SlideshowStatus
{
    public static string Format(bool isAdvancing, TimeSpan? remaining)
    {
        if (isAdvancing)
        {
            return "Changing wallpapers…";
        }

        if (remaining is null)
        {
            return "Slideshow off";
        }

        var seconds = Math.Max(0, (int)Math.Ceiling(remaining.Value.TotalSeconds));
        var time = TimeSpan.FromSeconds(seconds);
        return seconds >= 3600
            ? $"Next change in {(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}"
            : $"Next change in {time.Minutes:00}:{time.Seconds:00}";
    }
}


public sealed class SlideshowTimer : IDisposable
{
    private readonly SettingsRepository settings;
    private readonly WallpaperActions wallpaperActions;
    private readonly DispatcherTimer timer;
    private readonly Func<DateTimeOffset> utcNow;
    private bool started;

    public SlideshowTimer(
        SettingsRepository settings,
        WallpaperActions wallpaperActions,
        DispatcherTimer? dispatcherTimer = null,
        Func<DateTimeOffset>? utcNow = null)
    {
        this.settings = settings;
        this.wallpaperActions = wallpaperActions;
        timer = dispatcherTimer ?? new DispatcherTimer();
        this.utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
        timer.Tick += OnTick;
    }

    public int IntervalSeconds => settings.Current.Slideshow.IntervalSeconds;
    public DateTimeOffset? NextChangeUtc { get; private set; }
    public bool IsAdvancing { get; private set; }
    public TimeSpan? Remaining
    {
        get
        {
            if (NextChangeUtc is not { } nextChange)
            {
                return null;
            }

            var remaining = nextChange - utcNow();
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
    }

    public event Action<SlideshowRunResult>? Completed;
    public event Action? ScheduleChanged;

    public void Start()
    {
        started = true;
        Reset();
    }

    public void SetIntervalSeconds(int seconds)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(seconds);
        var current = settings.Current;
        settings.Save(current with
        {
            Slideshow = current.Slideshow with { IntervalSeconds = seconds }
        });
        Reset();
    }

    public void Reset()
    {
        timer.Stop();
        NextChangeUtc = null;
        if (started && IntervalSeconds > 0)
        {
            timer.Interval = TimeSpan.FromSeconds(IntervalSeconds);
            NextChangeUtc = utcNow() + timer.Interval;
            timer.Start();
        }

        ScheduleChanged?.Invoke();
    }

    public async Task<SlideshowRunResult> AdvanceFolderDisplaysAsync()
    {
        timer.Stop();
        NextChangeUtc = null;
        IsAdvancing = true;
        ScheduleChanged?.Invoke();
        try
        {
            var changed = new List<DisplayProfile>();
            var errors = new List<string>();
            var displayIds = settings.Current.Displays.Values
                .Where(profile => profile.Source == WallpaperSource.Folder)
                .Select(profile => profile.DisplayId)
                .ToArray();

            foreach (var displayId in displayIds)
            {
                try
                {
                    changed.Add(await wallpaperActions.NextAsync(displayId));
                }
                catch (Exception exception)
                {
                    errors.Add($"{displayId}: {exception.Message}");
                }
            }

            var result = new SlideshowRunResult(changed, errors);
            Completed?.Invoke(result);
            return result;
        }
        finally
        {
            IsAdvancing = false;
            Reset();
        }
    }

    public void Dispose()
    {
        started = false;
        timer.Stop();
        timer.Tick -= OnTick;
    }

    private async void OnTick(object? sender, EventArgs e) =>
        await AdvanceFolderDisplaysAsync();
}
