using Maestro.Application.Interfaces;
using Maestro.Application.Models;
using Maestro.Core.Errors;
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

    [Qt.Ignore]
    public static ErrorReporter Errors { get; set; } = null!;

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
    public void Play(string path) => Errors.Run(async () =>
    {
        var trimmedPath = path.Trim();
        var track = Track.FromFile(trimmedPath);
        await Source.PlayAsync(track);
        _loadedPath = trimmedPath;
        TrackTitle = track.Title;
        Playing = true;
    });

    public void TogglePlaying()
    {
        if (Playing)
        {
            Errors.Run(async () =>
            {
                await Source.PauseAsync();
                Playing = false;
            });
            return;
        }

        var path = TrackPath.Trim();
        if (path != "" && path != _loadedPath)
            Play(path);
        else if (_loadedPath != "")
            Errors.Run(async () =>
            {
                await Source.ResumeAsync();
                Playing = true;
            });
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
