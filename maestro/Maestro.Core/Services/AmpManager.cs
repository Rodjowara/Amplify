using Maestro.Core.Interfaces;
using Maestro.Core.Model;

namespace Maestro.Core.Services;

public class AmpManager : IAmpManager, IDisposable
{
    private readonly List<AmpConnection> _amps = new();

    public IReadOnlyList<AmpConnection> AmpList => _amps;

    public void Add(string connectionString)
    {
        // TODO: error handling
        var amp = new AmpConnection(connectionString);
        _amps.Add(amp);
    }

    public void Dispose()
    {
        foreach (var amp in _amps) 
        {
            amp.Dispose();
        }

        _amps.Clear();
    }
}
