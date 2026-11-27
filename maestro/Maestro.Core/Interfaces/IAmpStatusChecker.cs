namespace Maestro.Core.Interfaces;

public interface IAmpStatusChecker
{
    Task<bool> IsOnlineAsync(string ip);
}
