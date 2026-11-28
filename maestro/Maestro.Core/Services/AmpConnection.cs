using Amplify.Amp;
using Grpc.Core;
using Grpc.Net.Client;
using Maestro.Core.Errors;
using Maestro.Core.Models;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Maestro.Core.Services;

internal class AmpConnectionOptions
{
    public Uri ConnectionString { get; init; } = null!;
    public string Host => ConnectionString.Host;
    public uint SnapcastPort { get; set; }
}

internal class AmpConnection : IDisposable
{
    private static readonly TimeSpan CallTimeout = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(2);

    private readonly AmpConnectionOptions _options;
    private readonly GrpcChannel _channel;
    private readonly AmpService.AmpServiceClient _client;
    private readonly CancellationTokenSource _cancellation = new();

    public AmpConnection(AmpConnectionOptions options)
    {
        _options = options;

        _channel = GrpcChannel.ForAddress(_options.ConnectionString, new GrpcChannelOptions
        {
            HttpHandler = new SocketsHttpHandler
            {
                // TODO: move to settings
                KeepAlivePingDelay = TimeSpan.FromSeconds(5),
                KeepAlivePingTimeout = TimeSpan.FromSeconds(3),
                EnableMultipleHttp2Connections = true
            }
        });

        _client = new AmpService.AmpServiceClient(_channel);
    }

    public void Dispose()
    {
        _cancellation.Cancel();
        _channel.Dispose();
    }

    private async Task HandshakeAsync(CancellationToken ct)
    {
        // TODO: IOptions?
        var request = new HandshakeRequest
        {
            SnapcastHost = GetLocalIpAddress(),
            SnapcastPort = _options.SnapcastPort,
        };

        HandshakeResponse response;
        try
        {
            response = await _client.HandshakeAsync(request, deadline: DateTime.UtcNow + CallTimeout, cancellationToken: ct);
        }
        catch (RpcException exception)
        {
            throw new MaestroException(exception, new { amp = _options.Host });
        }

        if (!response.Success)
            throw new MaestroException(ErrorCode.AMP_Unexpected, new { amp = _options.Host, message = response.Error });
    }

    public async Task MonitorStateAsync(Action<bool> callback)
    {
        var ct = _cancellation.Token;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await HandshakeAsync(ct);

                var request = new MonitorStateRequest();

                using var call = _client.MonitorState(request, cancellationToken: ct);
                await foreach (var response in call.ResponseStream.ReadAllAsync(ct))
                {
                    callback(true);
                }
            }
            catch (RpcException) { }

            callback(false);

            try
            {
                await Task.Delay(ReconnectDelay, ct);
            }
            catch (OperationCanceledException) { return; }
        }
    }

    private string GetLocalIpAddress()
    {
        var address = NetworkInterface.GetAllNetworkInterfaces()
          .Where(x => x.OperationalStatus == OperationalStatus.Up)
          .Where(x => x.NetworkInterfaceType is NetworkInterfaceType.Ethernet or NetworkInterfaceType.Wireless80211)
          .Select(x => x.GetIPProperties())
          .OrderByDescending(x => x.GatewayAddresses.Any(y => !y.Address.Equals(IPAddress.Any)))
          .SelectMany(x => x.UnicastAddresses)
          .Select(x => x.Address)
          .Where(x => x.AddressFamily == AddressFamily.InterNetwork)
          .Where(x => !IPAddress.IsLoopback(x))
          .Where(x => !x.ToString().StartsWith("169.254"))
          .FirstOrDefault();

        if (address is null) return "127.0.0.1";

        return address.IsIPv4MappedToIPv6
          ? address.MapToIPv4().ToString()
          : address.ToString();
    }
}
