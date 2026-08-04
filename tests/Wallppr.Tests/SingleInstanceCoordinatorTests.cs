namespace Wallppr.Tests;

[TestClass]
public sealed class SingleInstanceCoordinatorTests
{
    [TestMethod]
    public void Only_first_process_becomes_primary()
    {
        var applicationId = $"wallppr-test-{Guid.NewGuid():N}";
        using var primary = new SingleInstanceCoordinator(applicationId);

        var secondaryIsPrimary = RunOnDedicatedThread(() =>
        {
            using var secondary = new SingleInstanceCoordinator(applicationId);
            return secondary.IsPrimary;
        });

        Assert.IsTrue(primary.IsPrimary);
        Assert.IsFalse(secondaryIsPrimary);
    }

    [TestMethod]
    public void Secondary_launch_signals_primary()
    {
        var applicationId = $"wallppr-test-{Guid.NewGuid():N}";
        using var primary = new SingleInstanceCoordinator(applicationId);
        using var activated = new ManualResetEventSlim();
        primary.ActivationRequested += activated.Set;
        primary.StartListening();

        RunOnDedicatedThread(() =>
        {
            using var secondary = new SingleInstanceCoordinator(applicationId);
            secondary.SignalPrimary();
            return 0;
        });

        Assert.IsTrue(activated.Wait(TimeSpan.FromSeconds(2)));
    }

    [TestMethod]
    public void Disposing_primary_allows_next_launch_to_become_primary()
    {
        var applicationId = $"wallppr-test-{Guid.NewGuid():N}";
        var primary = new SingleInstanceCoordinator(applicationId);
        primary.Dispose();

        using var replacement = new SingleInstanceCoordinator(applicationId);

        Assert.IsTrue(replacement.IsPrimary);
    }

    private static T RunOnDedicatedThread<T>(Func<T> action)
    {
        T? result = default;
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                result = action();
            }
            catch (Exception exception)
            {
                error = exception;
            }
        });
        thread.Start();
        thread.Join();
        if (error is not null)
        {
            throw error;
        }
        return result!;
    }
}
