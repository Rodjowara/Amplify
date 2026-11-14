using Maestro.Core.Errors;
using Maestro.Core.Services;

namespace Maestro.Core.Tests;

public class AmpConnectionTests
{
    [Theory]
    [InlineData("")]
    [InlineData("not a url")]
    [InlineData("ftp://amp:50051")]
    [InlineData("amp:50051")]
    public void Invalid_address_throws_invalid_amp_address(string address)
    {
        var ex = Assert.Throws<MaestroException>(() => new AmpManager().Add(address));

        Assert.Equal(ErrorCode.InvalidAmpAddress, ex.Code);
        Assert.Equal(address, ex.Args["address"]);
    }

    [Fact]
    public void Valid_address_is_added()
    {
        using var manager = new AmpManager();

        manager.Add("http://127.0.0.1:50051");

        Assert.Single(manager.AmpList);
    }
}
