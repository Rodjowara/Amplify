using Amplify.Dummy;
using Grpc.Core;
using Grpc.Net.Client;
using Maestro.Core.Errors;

namespace Maestro.Core.Services;

public class AmpConnection : IDisposable
{
    // How long a call may take before it fails with ErrorCode.AmpTimeout
    private static readonly TimeSpan CallTimeout = TimeSpan.FromSeconds(3);

    private readonly string _address;
    private readonly GrpcChannel _channel;
    private readonly DummyService.DummyServiceClient _client;

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

        _channel = GrpcChannel.ForAddress(connectionString, new GrpcChannelOptions
        {
            // TODO ?
        });

        _client = new DummyService.DummyServiceClient(_channel);
    }

    public void Dispose() => _channel.Dispose();

    public Task<PingResponse> PingAsync(string message, CancellationToken ct = default)
        => CallAsync(deadline => _client.PingAsync(
            new PingRequest { Message = message }, deadline: deadline, cancellationToken: ct).ResponseAsync);

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
}
