using Maestro.Core.Errors;
using Maestro.Core.Interfaces;
using Qt.Quick;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Maestro.Desktop;
// TODO: Rename to match the Amp naming convention.
//       myb (*Bridge.cs, *Backend.cs).

[QmlElement]
public class DummyViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    [Qt.Ignore]
    public static IAmpManager Manager { get; set; } = null!;

    [Qt.Ignore]
    public static IErrorReporter Errors { get; set; } = null!;

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

    // TODO: start/stop the playback source once it exists (#75).
    private bool _playing = false;
    public bool Playing
    {
        get => _playing;
        set
        {
            if (_playing == value)
                return;
            _playing = value;
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
        var response = await Manager.AmpList.First().PingAsync("ping");
        PingResponse = response.Message;
    });

    protected virtual void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
