using Amplify.Amp;
using Grpc.Core;
using Grpc.Net.Client;
using Maestro.Core.Errors;
using System.Net;
using System.Net.Sockets;

namespace Maestro.Core.Services;

public class AmpConnection : IDisposable
{
    // How long a call may take before it fails with ErrorCode.AmpTimeout
    private static readonly TimeSpan CallTimeout = TimeSpan.FromSeconds(3);

    public string Host { get; }
    public int Port { get; }

    private readonly string _address;
    private readonly GrpcChannel _channel;
    private readonly AmpService.AmpServiceClient _client;

    public AmpConnection(string connectionString)
    {
        if (!Uri.TryCreate(connectionString, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new MaestroException(
                ErrorCode.InvalidAmpAddress,
                $"'{connectionString}' is not a valid amp address (expected http(s)://host:port).",
                MaestroException.Arg("address", connectionString));
        }

        _address = connectionString;
        Host = uri.Host;
        Port = uri.Port;

        _channel = GrpcChannel.ForAddress(connectionString, new GrpcChannelOptions
        {
            // TODO ?
        });

        _client = new AmpService.AmpServiceClient(_channel);
    }

    public void Dispose() => _channel.Dispose();

    public async Task HandshakeAsync(CancellationToken ct = default)
    {
        // TODO: IOptions?
        var request = new HandshakeRequest()
        {
            SnapcastHost = GetLocalIpAddress(),
            SnapcastPort = 1704
        };

        var response = await CallAsync(deadline =>
            _client.HandshakeAsync(request, deadline: deadline, cancellationToken: ct).ResponseAsync);

        if (!response.Success)
        {
            throw new MaestroException(
                ErrorCode.AmpRejectedRequest,
                $"Amp {_address} rejected the handshake: {response.Error}",
                new Dictionary<string, object?> { ["amp"] = _address, ["detail"] = response.Error });
        }
    }

    /// <summary>
    /// Runs one gRPC call with a deadline, and turns gRPC failures into <see cref="MaestroException"/>s.
    /// Use this for every call to the amp.
    /// </summary>
    private async Task<T> CallAsync<T>(Func<DateTime, Task<T>> call)
    {
        try
        {
            return await call(DateTime.UtcNow + CallTimeout);
        }
        catch (RpcException ex) when (ex.StatusCode != StatusCode.Cancelled)
        {
            throw GrpcErrorMapper.Map(ex, _address);
        }
    }

    private string GetLocalIpAddress()
    {
        try
        {
            using var socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            socket.Connect(Host, Port);

            var address = ((IPEndPoint)socket.LocalEndPoint!).Address;

            return address.IsIPv4MappedToIPv6
                ? address.MapToIPv4().ToString()
                : address.ToString();
        }
        catch (SocketException ex)
        {
            // No route to the amp (e.g. not on the same network, or its host name can't be resolved).
            throw new MaestroException(
                ErrorCode.AmpUnreachable,
                $"No network route to amp {_address}: {ex.Message}",
                MaestroException.Arg("amp", _address),
                ex);
        }
    }
}
