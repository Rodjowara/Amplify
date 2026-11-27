using Maestro.Core.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Maestro.Desktop.ViewModels;

public class AmpViewModel : BaseViewModel, INotifyPropertyChanged, IDisposable
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly Amp _amp;

    internal Guid Id => _amp.Id;
    public string Name => _amp.Name;
    public string Ip => _amp.Ip;
    public bool Online => _amp.IsOnline;

    internal AmpViewModel(Amp amp)
    {
        _amp = amp;
        _amp.StatusChanged += OnStatusChanged;
    }

    public void Dispose()
    {
        _amp.StatusChanged -= OnStatusChanged;
    }

    private void OnStatusChanged() => OnPropertyChanged(nameof(Online));

    protected virtual void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
