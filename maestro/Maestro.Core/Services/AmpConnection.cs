using Amplify.Amp;
using Grpc.Core;
using Grpc.Net.Client;
using Maestro.Core.Errors;
using System.Net;
using System.Net.Sockets;

namespace Maestro.Core.Services;

public class AmpConnection : IDisposable
{
    private static readonly TimeSpan CallTimeout = TimeSpan.FromSeconds(3);

    public string Host { get; }
    public int Port { get; }
    private string Address => $"{Host}:{Port}";

    private readonly GrpcChannel _channel;
    private readonly AmpService.AmpServiceClient _client;

    public AmpConnection(string connectionString)
    {
        if (!Uri.TryCreate(connectionString, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new MaestroException(ErrorCode.InvalidAmpAddress, new { address = connectionString });

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

        HandshakeResponse response;
        try
        {
            response = await _client.HandshakeAsync(request, deadline: DateTime.UtcNow + CallTimeout);
        }
        catch (RpcException exception)
        {
            throw new MaestroException(exception, new { amp = Address, message = exception.Status.Detail });
        }

        if (!response.Success)
            throw new MaestroException(ErrorCode.AmpRejectedRequest, new { amp = Address, message = response.Error });
    }

    private string GetLocalIpAddress()
    {
        using var socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
        try
        {
            socket.Connect(Host, Port);
        }
        catch (SocketException exception)
        {
            throw new MaestroException(ErrorCode.AmpUnreachable, new { amp = Address, message = exception.Message }, exception);
        }

        var address = ((IPEndPoint)socket.LocalEndPoint!).Address;

        return address.IsIPv4MappedToIPv6
            ? address.MapToIPv4().ToString()
            : address.ToString();
    }
}
