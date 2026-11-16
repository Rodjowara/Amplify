using Amplify.Amp;
using Grpc.Net.Client;
using System.Net;
using System.Net.Sockets;

namespace Maestro.Core.Services;

public class AmpConnection : IDisposable
{
    public string Host { get; }
    public int Port { get; }

    private readonly GrpcChannel _channel;
    private readonly AmpService.AmpServiceClient _client;

    public AmpConnection(string connectionString)
    {
        var uri = new Uri(connectionString);
        Host = uri.Host;
        Port = uri.Port;

        _channel = GrpcChannel.ForAddress(connectionString, new GrpcChannelOptions
        {
            // TODO ?
        });

        _client = new AmpService.AmpServiceClient(_channel);
    }

    public void Dispose() => _channel.Dispose();

    public async Task HandshakeAsync()
    {
        // TODO: IOptions?
        var request = new HandshakeRequest()
        {
            SnapcastHost = GetLocalIpAddress(),
            SnapcastPort = 1704
        };

        var response = await _client.HandshakeAsync(request);
        if (!response.Success)
        {
            throw new InvalidOperationException(response.Error);
        }
    }

    private string GetLocalIpAddress()
    {
        using var socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
        socket.Connect(Host, Port);

        var address = ((IPEndPoint)socket.LocalEndPoint!).Address;

        return address.IsIPv4MappedToIPv6
            ? address.MapToIPv4().ToString()
            : address.ToString();
    }
}
