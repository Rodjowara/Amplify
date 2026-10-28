using Maestro.Application.Interfaces;
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
    public static IDummyService Service { get; set; } = null!;

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

    public void Ping() => _ = PingAsync();

    private async Task PingAsync()
    {
        try
        {
            PingResponse = await Service.PingAsync("ping");
        }
        catch (Exception ex)
        {
            PingResponse = ex.Message;
        }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
