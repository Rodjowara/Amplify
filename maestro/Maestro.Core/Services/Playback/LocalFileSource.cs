using System.ComponentModel;
using System.Diagnostics;
using System.Net.Sockets;
using Maestro.Application.Interfaces;
using Maestro.Core.Errors;
using Maestro.Application.Models;

namespace Maestro.Infrastructure.Services.Playback;

/// <summary>
/// Plays local audio files: ffmpeg decodes the file to raw PCM, and the PCM is sent over TCP
/// to the snapserver stream described by <see cref="SnapcastStreamOptions"/>.
/// Needs <c>ffmpeg</c> on the PATH.
/// Failures while starting throw <see cref="MaestroException"/>; failures while playing go to <paramref name="reporter"/>.
/// </summary>
/// <param name="stream">The snapserver stream to send the audio to.</param>
/// <param name="reporter">Receives errors that happen in the background.</param>
public sealed class LocalFileSource(SnapcastStreamOptions stream, IErrorReporter reporter)
    : IPlaybackSource, IAsyncDisposable
{
    // Makes sure only one PlayAsync/Stop runs at a time.
    private readonly SemaphoreSlim _lock = new(1, 1);

    private CancellationTokenSource? _playback;
    private Task _pump = Task.CompletedTask;

    public async Task PlayAsync(Track track, CancellationToken ct = default)
    {
        if (!track.Location.IsFile)
            throw new MaestroException(
                ErrorCode.UnsupportedTrackSource,
                $"LocalFileSource can only play file URIs, got '{track.Location}'.",
                MaestroException.Arg("location", track.Location.ToString()));
        if (!File.Exists(track.Location.LocalPath))
            throw new MaestroException(
                ErrorCode.FileNotFound,
                $"Audio file not found: '{track.Location.LocalPath}'.",
                MaestroException.Arg("path", track.Location.LocalPath));

        await _lock.WaitAsync(ct);
        try
        {
            await StopCoreAsync();

            // Connect first: if snapserver isn't reachable, fail before starting ffmpeg.
            var tcp = new TcpClient();
            try
            {
                try
                {
                    await tcp.ConnectAsync(stream.Host, stream.Port, ct);
                }
                catch (SocketException ex)
                {
                    throw new MaestroException(
                        ErrorCode.SnapserverUnreachable,
                        $"Could not connect to snapserver at {stream.Host}:{stream.Port}: {ex.Message}",
                        inner: ex);
                }

                var ffmpeg = StartFfmpeg(track.Location.LocalPath);

                _playback = new CancellationTokenSource();
                // Runs in the background until the track ends or is replaced/stopped.
                _pump = PumpAsync(ffmpeg, tcp, _playback.Token);
            }
            catch
            {
                tcp.Dispose();
                throw;
            }
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
        try { await StopCoreAsync(); }
        finally { _lock.Release(); }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _lock.Dispose();
    }

    private async Task StopCoreAsync()
    {
        _playback?.Cancel();
        await _pump; // PumpAsync never throws, so this just waits for cleanup.
        _playback?.Dispose();
        _playback = null;
    }

    /// <summary>Starts ffmpeg so that it writes the decoded file to stdout as raw PCM in the stream's format.</summary>
    private Process StartFfmpeg(string path)
    {
        var info = new ProcessStartInfo("ffmpeg")
        {
            RedirectStandardOutput = true,
            CreateNoWindow = true,
        };

        // -re: read at real-time speed, so the whole file isn't pushed into the socket at once.
        // -vn: ignore video streams (e.g. album art embedded in mp3/m4a).
        // s16le: signed 16-bit little-endian PCM, which matches sampleformat ...:16:...
        string[] args =
        [
            "-hide_banner", "-loglevel", "error",
            "-re", "-i", path, "-vn",
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
            throw new MaestroException(
                ErrorCode.FfmpegNotFound,
                "ffmpeg was not found. Install it and make sure it is on the PATH.",
                inner: ex);
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
                var error = new MaestroException(
                    ErrorCode.StreamInterrupted,
                    $"Stream to snapserver ended: {ex.Message}",
                    inner: ex);

                reporter.Report(error, ErrorSeverity.Warning);
            }
            finally
            {
                if (!ffmpeg.HasExited)
                    ffmpeg.Kill(entireProcessTree: true);
            }
        }
    }
}
