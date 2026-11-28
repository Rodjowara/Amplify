using Maestro.Core.Models;

namespace Maestro.Core.Tests.Models;

public class TrackTests
{
    [Fact]
    public void FromFile_RelativePath_UsesFileNameAsTitleAndAbsoluteLocation()
    {
        var track = Track.FromFile(Path.Combine("music", "song.mp3"));

        Assert.Equal("song", track.Title);
        Assert.True(track.Location.IsFile);
        Assert.Equal(Path.GetFullPath(Path.Combine("music", "song.mp3")), track.Location.LocalPath);
    }
}