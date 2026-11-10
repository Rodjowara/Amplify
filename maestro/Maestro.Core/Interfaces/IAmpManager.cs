using Maestro.Core.Services;

namespace Maestro.Core.Interfaces;

public interface IAmpManager
{
    IReadOnlyList<AmpConnection> AmpList { get; }

    void Add(string connectionString);

    void Clear();
}
