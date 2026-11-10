namespace Maestro.Application.Models;

/// <summary>
/// One snapserver input stream. Used by both the snapserver config and the source that feeds the stream,
/// so the two always agree on port and PCM format.
/// </summary>
/// <param name="Name">Stream name shown to snapcast clients.</param>
/// <param name="Host">Host the source connects to in order to send PCM.</param>
/// <param name="Port">TCP port snapserver listens on for this stream's PCM input.</param>
/// <param name="SampleRate">Sample rate in Hz.</param>
/// <param name="BitDepth">Bits per sample (16 = signed 16-bit little endian).</param>
/// <param name="Channels">Number of channels (2 = stereo).</param>
/// <param name="ClientPort">TCP port snapserver listens on for snapcast clients (the amps).</param>
public sealed record SnapcastStreamOptions(
    string Name = "Local",
    string Host = "127.0.0.1",
    int Port = 4953,
    int SampleRate = 48000,
    int BitDepth = 16,
    int Channels = 2,
    int ClientPort = 1704)
{
    /// <summary>
    /// The <c>source = ...</c> value for snapserver.conf. In <c>mode=server</c> snapserver
    /// listens on <see cref="Port"/> and reads raw PCM from whoever connects.
    /// </summary>
    public string ToSourceUri() =>
        $"tcp://0.0.0.0:{Port}?name={Name}&mode=server&sampleformat={SampleRate}:{BitDepth}:{Channels}&codec=flac";
}
