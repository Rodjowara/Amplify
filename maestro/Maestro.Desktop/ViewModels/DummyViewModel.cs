using Maestro.Core.Errors;
using Maestro.Core.Interfaces;
using Qt.Quick;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Maestro.Desktop.ViewModels;
// TODO: Rename to match the Amp naming convention.
//       myb (*Bridge.cs, *Backend.cs).

[QmlElement]
public class DummyViewModel : BaseViewModel, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    [Qt.Ignore]
    public static IAmpManager Manager = GetRequiredService<IAmpManager>();

    [Qt.Ignore]
    public static ErrorReporter Errors { get; set; } = null!;

    private int _clicks = 0;
    public int Clicks
    {
        get => _clicks;
        set
        {
            if (_clicks == value)
                return;
            _clicks = value;
            OnPropertyChanged();
        }
    }

    private string _pingResponse = "";
    public string PingResponse
    {
        get => _pingResponse;
        private set
        {
            if (_pingResponse == value)
                return;
            _pingResponse = value;
            OnPropertyChanged();
        }
    }

    public void Ping() => Errors.Run(async () =>
    {
        // TODO: connection should be mapped to UI amp model
        var amp = Manager.AmpList.First();
        await amp.HandshakeAsync();
        PingResponse = $"Connected to {amp.Host}:{amp.Port}";
    });

    protected virtual void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
