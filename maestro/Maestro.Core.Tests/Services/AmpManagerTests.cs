using Maestro.Core.Errors;
using Maestro.Core.Services;

namespace Maestro.Core.Tests.Services;

public class AmpManagerTests
{
    private readonly AmpManager _manager = new();
    private readonly List<AmpConnection> _added = [];

    public AmpManagerTests() => _manager.Added += _added.Add;

    [Theory]
    [InlineData("")]
    [InlineData("amp")]
    [InlineData("256.0.0.1")]
    [InlineData("192.168.1")]
    [InlineData("::1")]
    [InlineData(" 10.0.0.1")]
    public async Task AddAsync_InvalidIp_ThrowsInvalidAddress(string ip)
    {
        var exception = await Assert.ThrowsAsync<MaestroException>(() => _manager.AddAsync("amp", ip));

        Assert.Equal(ErrorCode.AMP_InvalidAddress, exception.Code);
        Assert.Empty(_added);
    }
}