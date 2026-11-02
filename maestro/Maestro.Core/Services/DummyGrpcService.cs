using Amplify.Dummy;
using Grpc.Net.Client;
using Maestro.Application.Interfaces;

namespace Maestro.Infrastructure.Services;

public sealed class DummyGrpcService : IDummyService, IDisposable
{
    private readonly GrpcChannel _channel;
    private readonly DummyService.DummyServiceClient _client;

    public DummyService.DummyServiceClient Client => _client;

    public DummyGrpcService(string address)
    {
        _channel = GrpcChannel.ForAddress(address);
        _client = new DummyService.DummyServiceClient(_channel);
    }

    public async Task<string> PingAsync(string message, CancellationToken ct = default)
    {
        var response = await Client.PingAsync(new PingRequest { Message = message }, cancellationToken: ct);

        return response.Message;
    }

    public void Dispose() => _channel.Dispose();
}
