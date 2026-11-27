using Maestro.Core.Errors;
using Maestro.Core.Interfaces;
using Maestro.Core.Models;
using System.Net;
using System.Net.Sockets;

namespace Maestro.Core.Services;

public class AmpManager : IAmpManager, IDisposable
{
    private readonly List<AmpConnection> _amps = new();
    public event Action<Amp>? Added;
    public event Action<Amp>? Removed;

    public Task AddAsync(string name, string ip)
    {
        ValidateIpAddress(ip);

        // TODO: read options
        var amp = new AmpConnection(new AmpConnectionOptions
        {
            Name = name,
            ConnectionString = new Uri($"http://{ip}:50051"),
            SnapcastPort = 1704
        });

        _amps.Add(amp);
        Added?.Invoke(amp.Amp);

        amp.StartMonitoring();
        return Task.CompletedTask;
    }

    public Task RemoveAsync(Guid id)
    {
        var amp = _amps.FirstOrDefault(amp => amp.Amp.Id == id);
        if (amp is null) return Task.CompletedTask;

        _amps.Remove(amp);
        amp.Dispose();

        Removed?.Invoke(amp.Amp);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _amps.ForEach(x => x.Dispose());
        _amps.Clear();
    }

    // TODO: test
    private void ValidateIpAddress(string ip)
    {
        if (!IPAddress.TryParse(ip, out var address)
            || address.AddressFamily != AddressFamily.InterNetwork
            || address.ToString() != ip)
        {
            throw new MaestroException(ErrorCode.AMP_InvalidAddress, new { address = ip });
        }

        if (_amps.Any(amp => amp.Amp.Ip == ip))
        {
            throw new MaestroException(ErrorCode.AMP_AlreadyAdded, new { address = ip });
        }
    }
}
