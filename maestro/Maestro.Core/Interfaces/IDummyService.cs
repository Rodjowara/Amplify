namespace Maestro.Core.Interfaces;

public interface IDummyService
{
    Task<string> PingAsync(string message, CancellationToken ct = default);
}
