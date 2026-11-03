using Amplify.Dummy;
using Grpc.Core;

namespace Dummy.Server;

public class ServerDummyService : DummyService.DummyServiceBase
{
    public override Task<PingResponse> Ping(PingRequest request, ServerCallContext context)
    {
        return Task.FromResult(new PingResponse { Message = $"pong: {request.Message}" });
    }
}
