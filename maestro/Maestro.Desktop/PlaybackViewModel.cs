using Maestro.Application.Interfaces;
using Maestro.Application.Models;
using Qt.Quick;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Maestro.Desktop;

/// <summary>Debug UI for testing playback: play a local file path through the injected source.</summary>
[QmlElement]
public class PlaybackViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    // Set in Program.cs (same static pattern as DummyViewModel, since QML creates the instance).
    [Qt.Ignore]
    public static IPlaybackSource Source { get; set; } = null!;

    private string _status = "";
    /// <summary>Last result or error, shown in the UI.</summary>
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

    /// <summary>Called from QML. Starts playback without blocking the UI thread.</summary>
    public void Play(string path) => _ = PlayAsync(path);

    private async Task PlayAsync(string path)
    {
        try
        {
            var track = Track.FromFile(path.Trim());
            await Source.PlayAsync(track);
            Status = $"Playing: {track.Title}";
        }
        catch (Exception ex)
        {
            Status = ex.Message;
        }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
