namespace Wallppr;

public sealed class SingleInstanceCoordinator : IDisposable
{
    private readonly Mutex instanceMutex;
    private readonly EventWaitHandle activationEvent;
    private RegisteredWaitHandle? activationWait;
    private bool disposed;

    public SingleInstanceCoordinator(string applicationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationId);
        var name = $@"Local\{applicationId}";
        instanceMutex = new Mutex(false, $"{name}.Instance");
        try
        {
            IsPrimary = instanceMutex.WaitOne(0);
        }
        catch (AbandonedMutexException)
        {
            IsPrimary = true;
        }

        try
        {
            activationEvent = new EventWaitHandle(false, EventResetMode.AutoReset, $"{name}.Activate");
        }
        catch
        {
            if (IsPrimary)
            {
                instanceMutex.ReleaseMutex();
            }
            instanceMutex.Dispose();
            throw;
        }
    }

    public bool IsPrimary { get; }

    public event Action? ActivationRequested;

    public void StartListening()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (!IsPrimary)
        {
            throw new InvalidOperationException("Only the primary instance can listen for activation.");
        }

        activationWait ??= ThreadPool.RegisterWaitForSingleObject(
            activationEvent,
            (_, _) => ActivationRequested?.Invoke(),
            null,
            Timeout.Infinite,
            executeOnlyOnce: false);
    }

    public void SignalPrimary()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        activationEvent.Set();
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        activationWait?.Unregister(null);
        activationEvent.Dispose();
        if (IsPrimary)
        {
            instanceMutex.ReleaseMutex();
        }
        instanceMutex.Dispose();
    }
}
