namespace Maestro.Application.Interfaces;

public interface IDummyService
{
    Task<string> PingAsync(string message, CancellationToken ct = default);
}
