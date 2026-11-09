using Maestro.Application.Models;

namespace Maestro.Application.Interfaces;

/// <summary>
/// A source that can bring a track into its own snapserver stream (e.g. local files, Spotify, Apple Music, etc.).
/// It's a command interface: the source decides how its audio reaches snapserver,
/// so audio doesn't have to pass through Maestro (Spotify audio goes through librespot).
/// </summary>
public interface IPlaybackSource
{
    /// <summary>
    /// Starts playing <paramref name="track"/>. Returns once playback has started, not when it ends.
    /// Calling it again replaces the track that is currently playing.
    /// </summary>
    Task PlayAsync(Track track, CancellationToken ct = default);
}