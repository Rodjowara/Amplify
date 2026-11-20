using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;
using Maestro.Application.Interfaces;
using Maestro.Application.Models;
using Maestro.Core.Errors;

namespace Maestro.Infrastructure.Services.Playback;

/// <summary>
/// Plays local audio files: ffmpeg decodes the file to raw PCM, and the PCM is sent over TCP
/// to the snapserver stream described by <see cref="SnapcastStreamOptions"/>.
/// Needs <c>ffmpeg</c> on the PATH.
/// </summary>
public sealed class LocalFileSource(SnapcastStreamOptions stream, ErrorReporter errors) : IPlaybackSource, IAsyncDisposable
{
    // Makes sure only one PlayAsync/Stop runs at a time.
    private readonly SemaphoreSlim _lock = new(1, 1);

    private CancellationTokenSource? _playback;
    private Task _pump = Task.CompletedTask;

    private Track? _track;
    private TimeSpan _startOffset;
    private readonly Stopwatch _sinceStart = new();

    public async Task PlayAsync(Track track, CancellationToken ct = default)
    {
        if (!track.Location.IsFile)
            throw new ArgumentException($"LocalFileSource can only play file URIs, got '{track.Location}'.", nameof(track));
        if (!File.Exists(track.Location.LocalPath))
            throw new MaestroException(ErrorCode.FileNotFound, new { path = track.Location.LocalPath });

        await _lock.WaitAsync(ct);
        try
        {
            await StopCoreAsync();
            _track = track;
            _startOffset = TimeSpan.Zero;
            await StartCoreAsync(track, ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task PauseAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            if (_playback is null)
                return;
            _startOffset += _sinceStart.Elapsed;
            await StopCoreAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task ResumeAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            if (_track is null || _playback is not null)
                return;
            await StartCoreAsync(_track, ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Stops the current track, if any.</summary>
    public async Task StopAsync()
    {
        await _lock.WaitAsync();
        try
        {
            await StopCoreAsync();
            _track = null;
        }
        finally { _lock.Release(); }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _lock.Dispose();
    }

    /// <summary>Plays <paramref name="track"/> from <see cref="_startOffset"/>.</summary>
    private async Task StartCoreAsync(Track track, CancellationToken ct)
    {
        // Connect first: if snapserver isn't reachable, fail before starting ffmpeg.
        var tcp = new TcpClient();
        try
        {
            await ConnectToSnapserverAsync(tcp, ct);
            var ffmpeg = StartFfmpeg(track.Location.LocalPath, _startOffset);

            _playback = new CancellationTokenSource();
            _sinceStart.Restart();
            // Runs in the background until the track ends or is replaced/stopped.
            _pump = PumpAsync(ffmpeg, tcp, _playback.Token);
        }
        catch
        {
            tcp.Dispose();
            throw;
        }
    }

    private async Task StopCoreAsync()
    {
        _playback?.Cancel();
        await _pump; // PumpAsync never throws, so this just waits for cleanup.
        _playback?.Dispose();
        _playback = null;
    }

    private async Task ConnectToSnapserverAsync(TcpClient tcp, CancellationToken ct)
    {
        try
        {
            await tcp.ConnectAsync(stream.Host, stream.Port, ct);
        }
        catch (SocketException exception)
        {
            throw new MaestroException(ErrorCode.SnapserverUnreachable, new { message = exception.Message }, exception);
        }
    }

    /// <summary>Starts ffmpeg so that it writes the decoded file, from <paramref name="startOffset"/> on, to stdout as raw PCM in the stream's format.</summary>
    private Process StartFfmpeg(string path, TimeSpan startOffset)
    {
        var info = new ProcessStartInfo("ffmpeg")
        {
            RedirectStandardOutput = true,
            CreateNoWindow = true,
        };

        // -re: read at real-time speed, so the whole file isn't pushed into the socket at once.
        // -ss before -i: seek in the input, so resuming doesn't decode everything before the offset.
        // -vn: ignore video streams (e.g. album art embedded in mp3/m4a).
        // s16le: signed 16-bit little-endian PCM, which matches sampleformat ...:16:...
        string[] args =
        [
            "-hide_banner", "-loglevel", "error",
            "-re", "-ss", startOffset.TotalSeconds.ToString(CultureInfo.InvariantCulture),
            "-i", path, "-vn",
            "-f", $"s{stream.BitDepth}le",
            "-ar", stream.SampleRate.ToString(),
            "-ac", stream.Channels.ToString(),
            "pipe:1",
        ];
        foreach (var arg in args)
            info.ArgumentList.Add(arg);

        try
        {
            return Process.Start(info) ?? throw new InvalidOperationException("Could not start ffmpeg.");
        }
        catch (Win32Exception ex)
        {
            throw new MaestroException(ErrorCode.FfmpegNotFound, new { message = ex.Message }, ex);
        }
    }

    /// <summary>Copies ffmpeg's PCM output to snapserver until the file ends or playback is cancelled.</summary>
    private async Task PumpAsync(Process ffmpeg, TcpClient tcp, CancellationToken ct)
    {
        using (tcp)
        using (ffmpeg)
        {
            try
            {
                await ffmpeg.StandardOutput.BaseStream.CopyToAsync(tcp.GetStream(), ct);
            }
            catch (OperationCanceledException)
            {
                // Expects playback to be replaced or stopped
            }
            catch (Exception ex) when (ex is IOException or SocketException)
            {
                // snapserver closed the connection
                errors.Report(new MaestroException(ErrorCode.StreamInterrupted, new { message = ex.Message }, ex));
            }
            finally
            {
                if (!ffmpeg.HasExited)
                    ffmpeg.Kill(entireProcessTree: true);
            }
        }
    }
}
