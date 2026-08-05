using System.Windows.Threading;

namespace Wallppr.Tests;

[TestClass]
public sealed class SlideshowTimerTests
{
    [TestMethod]
    public void Start_with_zero_interval_stays_disabled_without_saving()
    {
        var store = new MemorySettingsStore();
        var dispatcherTimer = new DispatcherTimer();
        using var timer = CreateTimer(store, dispatcherTimer);

        timer.Start();

        Assert.IsFalse(dispatcherTimer.IsEnabled);
        Assert.AreEqual(0, store.SaveCount);
    }

    [TestMethod]
    public void Setting_positive_interval_schedules_full_interval_and_reset_does_not_save()
    {
        var store = new MemorySettingsStore();
        var dispatcherTimer = new DispatcherTimer();
        using var timer = CreateTimer(store, dispatcherTimer);
        timer.Start();

        timer.SetIntervalSeconds(30);

        Assert.IsTrue(dispatcherTimer.IsEnabled);
        Assert.AreEqual(TimeSpan.FromSeconds(30), dispatcherTimer.Interval);
        Assert.AreEqual(30, store.Settings.Slideshow.IntervalSeconds);
        Assert.AreEqual(1, store.SaveCount);

        timer.Reset();

        Assert.IsTrue(dispatcherTimer.IsEnabled);
        Assert.AreEqual(1, store.SaveCount);
    }

    [TestMethod]
    public void Negative_interval_is_rejected_without_replacing_active_value()
    {
        var store = new MemorySettingsStore(new WallpprSettings
        {
            Slideshow = new SlideshowSettings { IntervalSeconds = 15 }
        });
        var dispatcherTimer = new DispatcherTimer();
        using var timer = CreateTimer(store, dispatcherTimer);
        timer.Start();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => timer.SetIntervalSeconds(-1));

        Assert.AreEqual(15, store.Settings.Slideshow.IntervalSeconds);
        Assert.AreEqual(TimeSpan.FromSeconds(15), dispatcherTimer.Interval);
        Assert.IsTrue(dispatcherTimer.IsEnabled);
        Assert.AreEqual(0, store.SaveCount);
    }

    [TestMethod]
    public void Reset_tracks_deadline_in_memory_without_saving()
    {
        var now = new DateTimeOffset(2026, 8, 5, 18, 0, 0, TimeSpan.Zero);
        var store = new MemorySettingsStore(new WallpprSettings
        {
            Slideshow = new SlideshowSettings { IntervalSeconds = 30 }
        });
        var dispatcherTimer = new DispatcherTimer();
        using var timer = CreateTimer(store, dispatcherTimer, () => now);

        timer.Start();

        Assert.AreEqual(now.AddSeconds(30), timer.NextChangeUtc);
        Assert.AreEqual(TimeSpan.FromSeconds(30), timer.Remaining);
        Assert.AreEqual(0, store.SaveCount);

        now = now.AddSeconds(8);

        Assert.AreEqual(TimeSpan.FromSeconds(22), timer.Remaining);

        timer.Reset();

        Assert.AreEqual(now.AddSeconds(30), timer.NextChangeUtc);
        Assert.AreEqual(0, store.SaveCount);
    }

    [TestMethod]
    public async Task Advance_publishes_changing_then_freshly_scheduled_state()
    {
        var now = new DateTimeOffset(2026, 8, 5, 18, 0, 0, TimeSpan.Zero);
        var (folder, first, _) = CreateImageFolder();
        var profile = new DisplayProfile
        {
            DisplayId = "folder-1",
            Source = WallpaperSource.Folder,
            FolderPath = folder,
            CurrentFolderImagePath = first
        };
        var store = new MemorySettingsStore(new WallpprSettings
        {
            Slideshow = new SlideshowSettings { IntervalSeconds = 30 },
            Displays = new Dictionary<string, DisplayProfile> { [profile.DisplayId] = profile }
        });
        var repository = new SettingsRepository(store, store.Settings);
        var actions = new WallpaperActions(new FakeWallpaperPlatform(), repository, new WallpaperThumbnailCache(Path.Combine(folder, "cache")));
        using var timer = new SlideshowTimer(repository, actions, new DispatcherTimer(), () => now);
        timer.Start();
        var states = new List<(bool Advancing, DateTimeOffset? NextChangeUtc)>();
        timer.ScheduleChanged += () => states.Add((timer.IsAdvancing, timer.NextChangeUtc));

        try
        {
            await timer.AdvanceFolderDisplaysAsync();

            Assert.HasCount(2, states);
            Assert.IsTrue(states[0].Advancing);
            Assert.IsNull(states[0].NextChangeUtc);
            Assert.IsFalse(states[1].Advancing);
            Assert.AreEqual(now.AddSeconds(30), states[1].NextChangeUtc);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [TestMethod]
    [DataRow(false, -1, "Slideshow off")]
    [DataRow(true, 42, "Changing wallpapers…")]
    [DataRow(false, 42, "Next change in 00:42")]
    [DataRow(false, 3723, "Next change in 01:02:03")]
    public void Status_formats_scheduler_state(bool advancing, int remainingSeconds, string expected)
    {
        var remaining = remainingSeconds < 0
            ? (TimeSpan?)null
            : TimeSpan.FromSeconds(remainingSeconds);

        Assert.AreEqual(expected, SlideshowStatus.Format(advancing, remaining));
    }

    [TestMethod]
    public void Status_rounds_fractional_seconds_up()
    {
        Assert.AreEqual(
            "Next change in 00:42",
            SlideshowStatus.Format(false, TimeSpan.FromMilliseconds(41_001)));
    }

    [TestMethod]
    public async Task Advance_changes_each_folder_display_once_and_reports_completion()
    {
        var (folder, first, second) = CreateImageFolder();
        var profiles = new Dictionary<string, DisplayProfile>
        {
            ["folder-1"] = new()
            {
                DisplayId = "folder-1",
                Source = WallpaperSource.Folder,
                FolderPath = folder,
                CurrentFolderImagePath = first
            },
            ["folder-2"] = new()
            {
                DisplayId = "folder-2",
                Source = WallpaperSource.Folder,
                FolderPath = folder,
                CurrentFolderImagePath = first
            },
            ["image-1"] = new()
            {
                DisplayId = "image-1",
                Source = WallpaperSource.Image,
                ImagePath = first
            }
        };
        var store = new MemorySettingsStore(new WallpprSettings { Displays = profiles });
        var repository = new SettingsRepository(store, store.Settings);
        var platform = new FakeWallpaperPlatform();
        var actions = new WallpaperActions(platform, repository, new WallpaperThumbnailCache(Path.Combine(folder, "cache")));
        using var timer = new SlideshowTimer(repository, actions, new DispatcherTimer());
        SlideshowRunResult? completed = null;
        timer.Completed += result => completed = result;

        try
        {
            var result = await timer.AdvanceFolderDisplaysAsync();

            Assert.HasCount(2, result.Changed);
            Assert.IsEmpty(result.Errors);
            CollectionAssert.AreEquivalent(new[] { "folder-1", "folder-2" }, platform.SetDisplayIds);
            Assert.DoesNotContain("image-1", platform.SetDisplayIds);
            Assert.IsTrue(result.Changed.All(profile => profile.CurrentFolderImagePath == second));
            Assert.AreSame(result, completed);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [TestMethod]
    public async Task Advance_continues_after_one_display_fails()
    {
        var (folder, first, second) = CreateImageFolder();
        var profiles = new Dictionary<string, DisplayProfile>
        {
            ["folder-1"] = new()
            {
                DisplayId = "folder-1",
                Source = WallpaperSource.Folder,
                FolderPath = folder,
                CurrentFolderImagePath = first
            },
            ["folder-2"] = new()
            {
                DisplayId = "folder-2",
                Source = WallpaperSource.Folder,
                FolderPath = folder,
                CurrentFolderImagePath = first
            }
        };
        var store = new MemorySettingsStore(new WallpprSettings { Displays = profiles });
        var repository = new SettingsRepository(store, store.Settings);
        var platform = new FakeWallpaperPlatform { ThrowDisplayId = "folder-1" };
        var actions = new WallpaperActions(platform, repository, new WallpaperThumbnailCache(Path.Combine(folder, "cache")));
        using var timer = new SlideshowTimer(repository, actions, new DispatcherTimer());

        try
        {
            var result = await timer.AdvanceFolderDisplaysAsync();

            Assert.HasCount(1, result.Changed);
            Assert.AreEqual("folder-2", result.Changed.Single().DisplayId);
            Assert.AreEqual(second, result.Changed.Single().CurrentFolderImagePath);
            Assert.HasCount(1, result.Errors);
            StringAssert.Contains(result.Errors.Single(), "folder-1");
            CollectionAssert.AreEqual(new[] { "folder-1", "folder-2" }, platform.SetDisplayIds);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    private static (string Folder, string First, string Second) CreateImageFolder()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"wallppr-timer-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        var first = Path.Combine(folder, "a.jpg");
        var second = Path.Combine(folder, "b.jpg");
        File.WriteAllBytes(first, []);
        File.WriteAllBytes(second, []);
        return (folder, first, second);
    }

    private static SlideshowTimer CreateTimer(
        MemorySettingsStore store,
        DispatcherTimer dispatcherTimer,
        Func<DateTimeOffset>? utcNow = null)
    {
        var repository = new SettingsRepository(store, store.Settings);
        var actions = new WallpaperActions(
            new FakeWallpaperPlatform(),
            repository,
            new WallpaperThumbnailCache(Path.Combine(Path.GetTempPath(), $"wallppr-timer-cache-{Guid.NewGuid():N}")));
        return new SlideshowTimer(repository, actions, dispatcherTimer, utcNow);
    }

    private sealed class MemorySettingsStore(WallpprSettings? settings = null) : ISettingsStore
    {
        public WallpprSettings Settings { get; private set; } = settings ?? new();
        public int SaveCount { get; private set; }

        public WallpprSettings Load() => Settings;

        public void Save(WallpprSettings settings)
        {
            Settings = settings;
            SaveCount++;
        }
    }

    private sealed class FakeWallpaperPlatform : IWallpaperPlatform
    {
        public string? ThrowDisplayId { get; init; }
        public List<string> SetDisplayIds { get; } = [];

        public IReadOnlyList<MonitorWallpaper> GetMonitors() => [];

        public void SetWallpaper(string displayId, string imagePath)
        {
            SetDisplayIds.Add(displayId);
            if (displayId == ThrowDisplayId)
            {
                throw new InvalidOperationException("platform failure");
            }
        }
    }
}
