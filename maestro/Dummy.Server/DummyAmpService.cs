using Amplify.Amp;
using Grpc.Core;

namespace Dummy.Server;

// Simulates one amp: keeps its state in memory.
// Registered as a singleton, so the state survives between calls.
public class DummyAmpService : AmpService.AmpServiceBase
{
    private readonly object _lock = new();
    private readonly AmpState _state = new() { Name = "Dummy Amp" };

    public override Task<AmpState> GetState(GetStateRequest request, ServerCallContext context)
    {
        lock (_lock)
            return Task.FromResult(_state.Clone());
    }

    public override Task<AmpState> SetStream(SetStreamRequest request, ServerCallContext context)
    {
        lock (_lock)
        {
            _state.Stream = request.Stream;
            return Task.FromResult(_state.Clone());
        }
    }
}
