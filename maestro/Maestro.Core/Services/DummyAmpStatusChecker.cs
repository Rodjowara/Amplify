using Maestro.Core.Interfaces;

namespace Maestro.Core.Services;

public class DummyAmpStatusChecker : IAmpStatusChecker
{
    public Task<bool> IsOnlineAsync(string ip) => Task.FromResult(true);
}
