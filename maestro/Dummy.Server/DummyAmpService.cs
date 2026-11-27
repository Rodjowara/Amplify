using Amplify.Amp;
using Grpc.Core;

namespace Dummy.Server;

public class DummyAmpService(IHostApplicationLifetime lifetime) : AmpService.AmpServiceBase
{
    public override Task<HandshakeResponse> Handshake(HandshakeRequest request, ServerCallContext context)
    {
        return Task.FromResult(new HandshakeResponse { Success = true });
    }

    public override async Task Monitor(MonitorRequest request, IServerStreamWriter<MonitorResponse> responseStream, ServerCallContext context)
    {
        await responseStream.WriteAsync(new MonitorResponse());
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken, lifetime.ApplicationStopping);
        await Task.Delay(Timeout.Infinite, stop.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
    }
}
