namespace Maestro.Application.Models;

/// <summary>
/// A playable track. Location-Uri tells the source where to find it,
/// e.g. <c>file:///music/song.mp3</c> (local) or later <c>spotify:track:...</c>.
/// </summary>
public sealed record Track(string Id, string Title, Uri Location)
{
    /// <summary>Creates a track for a local audio file. The file name is used as the title.</summary>
    public static Track FromFile(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var trackTitle = Path.GetFileNameWithoutExtension(fullPath);
        return new Track(fullPath, trackTitle, new Uri(fullPath));
    }

    // #TODO: Add a FromSpotify(string spotifyUri) method for Spotify tracks, and similar for other sources.
}