using Maestro.Core.Models;

namespace Maestro.Core.Interfaces;

public interface IAmpManager
{
    event Action<Amp>? Added;
    event Action<Amp>? Removed;

    Task AddAsync(string name, string ip);
    Task RemoveAsync(Guid id);
}
