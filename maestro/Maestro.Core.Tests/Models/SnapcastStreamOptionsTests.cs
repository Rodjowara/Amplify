using Maestro.Core.Models;

namespace Maestro.Core.Tests.Models;

public class SnapcastStreamOptionsTests
{
    [Fact]
    public void ToSourceUri_Defaults_MatchesSnapserverFormat()
    {
        Assert.Equal(
            "tcp://0.0.0.0:4953?name=Local&mode=server&sampleformat=48000:16:2&codec=flac",
            new SnapcastStreamOptions().ToSourceUri());
    }
}