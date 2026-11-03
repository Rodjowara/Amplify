using System.Threading.Channels;
using Amplify.Amp;
using Grpc.Core;

namespace Dummy.Server;

// Simulates one amp: keeps its state in memory and pushes every change to WatchState subscribers.
// Registered as a singleton, so the state survives between calls.
public class DummyAmpService : AmpService.AmpServiceBase
{
    private const int EqBandCount = 10;

    private readonly object _lock = new();
    private readonly List<Channel<AmpState>> _watchers = new();
    private readonly AmpState _state = new()
    {
        Name = "Dummy Amp",
        Channels = { AudioChannel.Left, AudioChannel.Right },
        Volume = 1f,
        MasterVolume = 1f,
        CrossoverHz = 80f,
        EqGainsDb = { new float[EqBandCount] },
    };

    public override Task<AmpState> GetState(GetStateRequest request, ServerCallContext context)
    {
        lock (_lock)
            return Task.FromResult(_state.Clone());
    }

    public override async Task WatchState(WatchStateRequest request, IServerStreamWriter<AmpState> responseStream, ServerCallContext context)
    {
        var watcher = Channel.CreateUnbounded<AmpState>();
        lock (_lock)
        {
            _watchers.Add(watcher);
            watcher.Writer.TryWrite(_state.Clone());
        }

        try
        {
            await foreach (var state in watcher.Reader.ReadAllAsync(context.CancellationToken))
                await responseStream.WriteAsync(state);
        }
        catch (OperationCanceledException)
        {
            // client disconnected
        }
        finally
        {
            lock (_lock)
                _watchers.Remove(watcher);
        }
    }

    public override Task<AmpState> SetName(SetNameRequest request, ServerCallContext context)
        => Update(s => s.Name = request.Name);

    public override Task<AmpState> SetChannels(SetChannelsRequest request, ServerCallContext context)
        => Update(s =>
        {
            s.Channels.Clear();
            s.Channels.AddRange(request.Channels);
        });

    public override Task<AmpState> SetVolume(SetVolumeRequest request, ServerCallContext context)
        => Update(s => s.Volume = Math.Clamp(request.Volume, 0f, 1f));

    public override Task<AmpState> SetMasterVolume(SetMasterVolumeRequest request, ServerCallContext context)
        => Update(s => s.MasterVolume = Math.Clamp(request.MasterVolume, 0f, 1f));

    public override Task<AmpState> SetMute(SetMuteRequest request, ServerCallContext context)
        => Update(s => s.Muted = request.Muted);

    public override Task<AmpState> SetSolo(SetSoloRequest request, ServerCallContext context)
        => Update(s => s.Solo = request.Solo);

    public override Task<AmpState> SetDelay(SetDelayRequest request, ServerCallContext context)
        => Update(s =>
        {
            s.DelayMs = Math.Max(0f, request.DelayMs);
            s.PhaseInverted = request.PhaseInverted;
        });

    public override Task<AmpState> SetCrossover(SetCrossoverRequest request, ServerCallContext context)
        => Update(s => s.CrossoverHz = Math.Max(0f, request.CrossoverHz));

    public override Task<AmpState> SetEqBand(SetEqBandRequest request, ServerCallContext context)
    {
        if (request.Band >= EqBandCount)
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"band must be 0-{EqBandCount - 1}"));

        return Update(s => s.EqGainsDb[(int)request.Band] = request.GainDb);
    }

    public override Task<AmpState> SetStream(SetStreamRequest request, ServerCallContext context)
        => Update(s => s.Stream = request.Stream);

    private Task<AmpState> Update(Action<AmpState> change)
    {
        lock (_lock)
        {
            change(_state);
            var snapshot = _state.Clone();
            foreach (var watcher in _watchers)
                watcher.Writer.TryWrite(snapshot.Clone());
            return Task.FromResult(snapshot);
        }
    }
}
