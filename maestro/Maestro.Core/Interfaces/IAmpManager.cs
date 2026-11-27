using Maestro.Core.Services;

namespace Maestro.Core.Interfaces;

public interface IAmpManager
{
    event Action<AmpConnection>? Added;
    event Action<AmpConnection>? Removed;

    Task AddAsync(string name, string ip);
    Task RemoveAsync(Guid id);
}
