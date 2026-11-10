using Maestro.Core.Interfaces;
using Qt.Quick;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Maestro.Desktop;

[QmlElement]
public class AmpViewModel : INotifyPropertyChanged
{
    private const int DefaultAmpPort = 50051;

    public event PropertyChangedEventHandler? PropertyChanged;

    [Qt.Ignore]
    public static IAmpManager Manager { get; set; } = null!;

    [Qt.Ignore]
    public static int StreamPort { get; set; }

    private string _status = "";
    public string Status
    {
        get => _status;
        private set
        {
            if (_status == value)
                return;
            _status = value;
            OnPropertyChanged();
        }
    }

    public void Connect(string address) => _ = ConnectAsync(address);

    private async Task ConnectAsync(string address)
    {
        try
        {
            var connectionString = ToConnectionString(address);
            Manager.Clear();
            Manager.Add(connectionString);

            Status = $"Connecting to {connectionString}...";
            var state = await Manager.AmpList[0].SetStreamAsync(StreamPort);
            Status = $"{state.Name} streaming from {state.Stream.Host}:{state.Stream.Port}";
        }
        catch (Exception ex)
        {
            Status = ex.Message;
        }
    }

    private static string ToConnectionString(string address)
    {
        address = address.Trim();
        if (!address.Contains("://"))
            address = "http://" + address;

        var uri = new UriBuilder(address);
        if (new Uri(address).IsDefaultPort)
            uri.Port = DefaultAmpPort;
        return uri.Uri.GetLeftPart(UriPartial.Authority);
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
