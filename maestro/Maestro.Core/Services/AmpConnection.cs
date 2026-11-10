using System.Net;
using System.Net.Sockets;
using Amplify.Amp;
using Amplify.Dummy;
using Grpc.Net.Client;

namespace Maestro.Core.Services;

public class AmpConnection : IDisposable
{
    private readonly GrpcChannel _channel;
    private readonly DummyService.DummyServiceClient _client;
    private readonly AmpService.AmpServiceClient _amp;
    private readonly Uri _uri;

    public AmpConnection(string connectionString)
    {
        _uri = new Uri(connectionString);

        _channel = GrpcChannel.ForAddress(connectionString, new GrpcChannelOptions
        {
            // TODO ?
        });

        _client = new DummyService.DummyServiceClient(_channel);
        _amp = new AmpService.AmpServiceClient(_channel);
    }

    public void Dispose() => _channel.Dispose();

    public async Task<PingResponse> PingAsync(string message)
    {
        return await _client.PingAsync(new PingRequest { Message = message });
    }

    public async Task<AmpState> GetStateAsync(CancellationToken ct = default)
    {
        return await _amp.GetStateAsync(new GetStateRequest(), cancellationToken: ct);
    }

    public async Task<AmpState> SetStreamAsync(int port, CancellationToken ct = default)
    {
        var host = await LocalAddressTowardAsync(_uri.Host, ct);
        var request = new SetStreamRequest
        {
            Stream = new StreamInfo { Host = host.ToString(), Port = (uint)port },
        };
        return await _amp.SetStreamAsync(request, cancellationToken: ct);
    }

    private static async Task<IPAddress> LocalAddressTowardAsync(string remoteHost, CancellationToken ct)
    {
        var remote = IPAddress.TryParse(remoteHost, out var ip)
            ? ip
            : (await Dns.GetHostAddressesAsync(remoteHost, AddressFamily.InterNetwork, ct)).First();

        if (IPAddress.IsLoopback(remote))
            return IPAddress.Loopback;

        using var socket = new Socket(remote.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
        socket.Connect(remote, 9);
        return ((IPEndPoint)socket.LocalEndPoint!).Address;
    }
}
