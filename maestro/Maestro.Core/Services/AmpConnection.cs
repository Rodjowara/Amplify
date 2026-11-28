using Amplify.Amp;
using Grpc.Core;
using Grpc.Net.Client;
using Maestro.Core.Errors;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Maestro.Core.Services;

public class AmpConnectionOptions
{
    public string Name { get; init; } = null!;

    public Uri ConnectionString { get; init; } = null!;
    public string Host => ConnectionString.Host;
    public int Port => ConnectionString.Port;

    public uint SnapcastPort { get; set; }
}

public class AmpConnection : IDisposable
{
    private static readonly TimeSpan CallTimeout = TimeSpan.FromSeconds(3);

    private readonly AmpConnectionOptions _options;
    public Guid Id { get; } = Guid.NewGuid();
    public string Name => _options.Name;
    public string Host => _options.Host;

    private readonly GrpcChannel _channel;
    private readonly AmpService.AmpServiceClient _client;

    public AmpConnection(AmpConnectionOptions options)
    {
        _options = options;

        _channel = GrpcChannel.ForAddress(_options.ConnectionString, new GrpcChannelOptions
        {
            // TODO ?
        });

        _client = new AmpService.AmpServiceClient(_channel);
    }

    public void Dispose() => _channel.Dispose();

    public async Task HandshakeAsync()
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
            response = await _client.HandshakeAsync(request, deadline: DateTime.UtcNow + CallTimeout);
        }
        catch (RpcException exception)
        {
            throw new MaestroException(exception, new { amp = _options.Host });
        }

        if (!response.Success)
            throw new MaestroException(ErrorCode.AMP_Unexpected, new { amp = _options.Host, message = response.Error });
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
