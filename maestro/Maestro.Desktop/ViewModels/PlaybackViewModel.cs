using Maestro.Core.Interfaces;
using Maestro.Core.Models;
using Maestro.Core.Errors;
using Maestro.Core.Services.Playback;
using Qt.Quick;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Maestro.Desktop.ViewModels;

/// <summary>Debug UI for testing playback: play a local file path through the injected source.</summary>
[QmlElement]
public class PlaybackViewModel : BaseViewModel, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly IPlaybackSource _source = GetRequiredService<LocalFileSource>();
    private readonly ErrorReporter _errors = GetRequiredService<ErrorReporter>();

    public string TrackPath { get; set; } = "";

    private string _loadedPath = "";

    private string _trackTitle = "";
    /// <summary>Title of the current track, shown in the playback dock.</summary>
    public string TrackTitle
    {
        get => _trackTitle;
        private set
        {
            if (_trackTitle == value)
                return;
            _trackTitle = value;
            OnPropertyChanged();
        }
    }

    private bool _playing;
    public bool Playing
    {
        get => _playing;
        private set
        {
            if (_playing == value)
                return;
            _playing = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Called from QML. Starts playback without blocking the UI thread.</summary>
    public void Play(string path) => _errors.Run(async () =>
    {
        var trimmedPath = path.Trim();
        var track = Track.FromFile(trimmedPath);
        await _source.PlayAsync(track);
        _loadedPath = trimmedPath;
        TrackTitle = track.Title;
        Playing = true;
    });

    public void TogglePlaying()
    {
        if (Playing)
        {
            _errors.Run(async () =>
            {
                await _source.PauseAsync();
                Playing = false;
            });
            return;
        }

        var path = TrackPath.Trim();
        if (path != "" && path != _loadedPath)
            Play(path);
        else if (_loadedPath != "")
            _errors.Run(async () =>
            {
                await _source.ResumeAsync();
                Playing = true;
            });
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
