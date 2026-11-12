using Amplify.Dummy;
using Grpc.Net.Client;

namespace Maestro.Core.Services;

public class AmpConnection : IDisposable
{
    private readonly GrpcChannel _channel;
    private readonly DummyService.DummyServiceClient _client;

    public AmpConnection(string connectionString)
    {
        var uri = new Uri(connectionString);
        var host = uri.Host;
        var port = uri.Port;

        _channel = GrpcChannel.ForAddress(connectionString, new GrpcChannelOptions
        {
            // TODO ?
        });

        _client = new DummyService.DummyServiceClient(_channel);
    }

    public void Dispose() => _channel.Dispose();

    public async Task<PingResponse> PingAsync(string message)
    {
        return await _client.PingAsync(new PingRequest { Message = message });
    }
}
