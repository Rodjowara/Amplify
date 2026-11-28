using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Maestro.Core.Errors;
using Maestro.Core.Models;
using Maestro.Core.Services.Playback;

namespace Maestro.Core.Tests.Services.Playback;

public class LocalFileSourceTests : IDisposable
{
    private const int IntegrationTimeoutMilliseconds = 10_000;
    private const int TestAudioSeconds = 5;
    private const int ReadBufferSize = 4096;

    private readonly string _audioPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.wav");

    public void Dispose() => File.Delete(_audioPath);

    [Fact]
    public async Task PlayAsync_NonFileUri_ThrowsArgumentException()
    {
        await using var source = CreateSource(port: 0);
        var track = new Track("id", "title", new Uri("spotify:track:123"));

        await Assert.ThrowsAsync<ArgumentException>(() => source.PlayAsync(track, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task PlayAsync_MissingFile_ThrowsFileNotFound()
    {
        await using var source = CreateSource(port: 0);

        var exception = await Assert.ThrowsAsync<MaestroException>(() => source.PlayAsync(Track.FromFile(_audioPath), TestContext.Current.CancellationToken));

        Assert.Equal(ErrorCode.FileNotFound, exception.Code);
    }

    [Fact]
    public async Task PauseAndResume_NothingPlaying_DoNothing()
    {
        await using var source = CreateSource(port: 0);

        await source.PauseAsync(TestContext.Current.CancellationToken);
        await source.ResumeAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task PlayAsync_NoSnapserverListening_ThrowsSnapserverUnreachable()
    {
        File.WriteAllBytes(_audioPath, []);
        await using var source = CreateSource(GetUnusedPort());

        var exception = await Assert.ThrowsAsync<MaestroException>(() => source.PlayAsync(Track.FromFile(_audioPath), TestContext.Current.CancellationToken));

        Assert.Equal(ErrorCode.SnapserverUnreachable, exception.Code);
    }

    [Fact(Timeout = IntegrationTimeoutMilliseconds)]
    public async Task PlayPauseResume_StreamsPcmToSnapserver()
    {
        await GenerateTestAudioAsync();
        using var snapserver = new TcpListener(IPAddress.Loopback, 0);
        snapserver.Start();
        await using var source = CreateSource(((IPEndPoint)snapserver.LocalEndpoint).Port);

        await source.PlayAsync(Track.FromFile(_audioPath), TestContext.Current.CancellationToken);
        using (var playing = await snapserver.AcceptTcpClientAsync(TestContext.Current.CancellationToken))
        {
            Assert.True(await ReadAsync(playing) > 0);

            await source.PauseAsync(TestContext.Current.CancellationToken);
            while (await ReadAsync(playing) > 0) { }
        }

        await source.ResumeAsync(TestContext.Current.CancellationToken);
        using var resumed = await snapserver.AcceptTcpClientAsync(TestContext.Current.CancellationToken);
        Assert.True(await ReadAsync(resumed) > 0);
    }

    private static LocalFileSource CreateSource(int port) =>
        new(new SnapcastStreamOptions(Port: port), new ErrorReporter());

    private static int GetUnusedPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static async Task<int> ReadAsync(TcpClient client) =>
        await client.GetStream().ReadAsync(new byte[ReadBufferSize], TestContext.Current.CancellationToken);

    private async Task GenerateTestAudioAsync()
    {
        var info = new ProcessStartInfo("ffmpeg");
        foreach (var arg in new[] { "-loglevel", "error", "-f", "lavfi", "-i", $"sine=duration={TestAudioSeconds}", _audioPath })
            info.ArgumentList.Add(arg);
        using var ffmpeg = Process.Start(info)!;
        await ffmpeg.WaitForExitAsync();
        Assert.Equal(0, ffmpeg.ExitCode);
    }
}