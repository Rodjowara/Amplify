using Maestro.Core.Services;
using Qt.Quick;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Maestro.Desktop.ViewModels;

[QmlElement]
public class SpotifyViewModel : BaseViewModel, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private const int SearchResultLimit = 10;

    private SpotifyConnection _connection;

    public SpotifyViewModel()
    {
        _connection = GetRequiredService<SpotifyConnection>();
    }

    private string _result = "";
    public string Result
    {
        get => _result;
        private set
        {
            if (_result == value)
                return;
            _result = value;
            OnPropertyChanged();
        }
    }

    public void Connect() => _ = _connection.ConnectAsync();

    public void Search(string query) => _ = SearchAsync(query);

    private async Task SearchAsync(string query)
    {
        try
        {
            var tracks = await _connection.SearchTracksAsync(query, SearchResultLimit);
            Result = JsonSerializer.Serialize(tracks);
        }
        catch (Exception ex)
        {
            Result = ex.Message;
        }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
