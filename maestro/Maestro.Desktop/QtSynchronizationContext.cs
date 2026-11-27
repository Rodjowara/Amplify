using Qt.Quick;
using System.Collections.Concurrent;
using System.ComponentModel;

[assembly: Qt.IgnoreType(typeof(SynchronizationContext))]
namespace Maestro.Desktop;

[QmlElement]
[Qt.Export(Options = Qt.ExportAs.Metadata)]
public class QtSynchronizationContext : SynchronizationContext, INotifyPropertyChanged, IDisposable
{
    private readonly ConcurrentQueue<Action> _queue = new();
    private readonly SynchronizationContext? _previousContext;
    private volatile bool _disposed;
    public int Pending => _queue.Count;

    public event PropertyChangedEventHandler? PropertyChanged;

    public QtSynchronizationContext()
    {
        _previousContext = Current;
        SetSynchronizationContext(this);
    }

    public override void Post(SendOrPostCallback callback, object? state)
    {
        if (_disposed)
        {
            return;
        }

        _queue.Enqueue(() => callback(state));
        PendingChanged();
    }

    public override SynchronizationContext CreateCopy()
    {
        return this;
    }

    public void Execute()
    {
        try
        {
            while (_queue.TryDequeue(out var work))
            {
                work();
            }
        }
        finally
        {
            if (!_queue.IsEmpty)
            {
                PendingChanged();
            }
        }
    }

    private void PendingChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Pending)));
    }

    public void Dispose()
    {
        _disposed = true;
        _queue.Clear();
        
        if (Current == this)
        {
            SetSynchronizationContext(_previousContext);
        }
    }
}
