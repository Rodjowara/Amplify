using Qt.Quick;
using System.Collections.Concurrent;
using System.ComponentModel;

namespace Maestro.Desktop;

[QmlElement]
public class QtSynchronizationContext : SynchronizationContext, INotifyPropertyChanged
{
    private ConcurrentQueue<Action> _queue = new();
    public int Pending => _queue.Count;
    
    public event PropertyChangedEventHandler? PropertyChanged;

    public QtSynchronizationContext()
    {
        SetSynchronizationContext(this);
    }

    public override void Post(SendOrPostCallback callback, object? state)
    {
        _queue.Enqueue(() => callback(state));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Pending)));
    }

    public override SynchronizationContext CreateCopy()
    {
        return this;
    }

    public void Execute()
    {
        while (_queue.TryDequeue(out var work))
        {
            try
            {
                work();
            }
            catch (Exception ex)
            {
                // TODO: show error message
                throw;
            }
        }
    }
}
