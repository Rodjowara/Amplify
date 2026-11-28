using Maestro.Core.Errors;
using Maestro.Core.Interfaces;
using Maestro.Core.Models;
using System.Net;
using System.Net.Sockets;

namespace Maestro.Core.Services;

public class AmpManager : IAmpManager, IDisposable
{
    private readonly Dictionary<Guid, Amp> _amps = new();
    private readonly Dictionary<Guid, AmpConnection> _connections = new();
    public event Action<Amp>? Added;
    public event Action<Amp>? Removed;

    public Task AddAsync(string name, string ip)
    {
        ValidateIpAddress(ip);

        var connection = new AmpConnection(new AmpConnectionOptions
        {
            ConnectionString = new Uri($"http://{ip}:50051"),
            SnapcastPort = 1704
        });

        var amp = new Amp(name, ip);

        _amps.Add(amp.Id, amp);
        _connections.Add(amp.Id, connection);

        Added?.Invoke(amp);

        _ = connection.MonitorStateAsync(amp.SetOnline);

        return Task.CompletedTask;
    }

    public Task RemoveAsync(Guid id)
    {
        if (!_amps.Remove(id, out var amp)) return Task.CompletedTask;

        _connections.Remove(id, out var connection);
        connection!.Dispose();

        Removed?.Invoke(amp);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        foreach (var connection in _connections.Values)
        {
            connection.Dispose();
        }

        _connections.Clear();
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

        if (_amps.Values.Any(x => x.Ip == ip))
        {
            throw new MaestroException(ErrorCode.AMP_AlreadyAdded, new { address = ip });
        }
    }
}
