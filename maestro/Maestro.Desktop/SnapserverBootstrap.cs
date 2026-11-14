using System.ComponentModel;
using System.Diagnostics;
using Maestro.Core.Errors;
using Maestro.Application.Models;

namespace Maestro.Desktop;

/// <summary>Runs snapserver in a Docker container, with one input stream defined by <see cref="SnapcastStreamOptions"/>.</summary>
public class SnapcastBootstrap(SnapcastStreamOptions stream) : IAsyncDisposable
{
    private readonly string _name = "maestro-snapserver";

    private string Command => string.Join(" && ",
          // install snapserver
          "apk add --no-cache snapcast-server",
          // snapserver config: one stream, built from the shared options
          $"printf '[stream]\\nsource = {stream.ToSourceUri()}\\n' > /etc/snapserver.conf",
          // start service
          "exec snapserver");

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        // NOTE: docker cleanup
        await ExecuteDockerAsync(["rm", "-f", _name], CancellationToken.None);

        var arguments = new List<string>();

        arguments.AddRange(["run", "-d"]);
        arguments.AddRange(["--name", _name]);

        // NOTE: audio input (PCM from the playback source, localhost only)
        arguments.AddRange(["-p", $"127.0.0.1:{stream.Port}:{stream.Port}"]);
        // NOTE: audio output (port 1704)
        arguments.AddRange(["-p", "1704:1704",]);

        arguments.Add("alpine:3.21");
        arguments.AddRange(["sh", "-c", Command]);

        await ExecuteDockerAsync(arguments, ct);
    }

    public async ValueTask DisposeAsync()
    {
        // Cleanup on shutdown must not throw, there is nobody left to handle it.
        try
        {
            await ExecuteDockerAsync(["rm", "-f", _name], CancellationToken.None);
        }
        catch (MaestroException ex)
        {
            Console.Error.WriteLine($"[SnapcastBootstrap] could not remove container '{_name}': {ex.Message}");
        }
    }

    private static async Task ExecuteDockerAsync(List<string> arguments, CancellationToken ct)
    {
        var info = new ProcessStartInfo("docker")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        arguments.ForEach(info.ArgumentList.Add);

        Process process;
        try
        {
            process = Process.Start(info)!;
        }
        catch (Win32Exception ex)
        {
            throw new MaestroException(
                ErrorCode.DockerNotAvailable,
                "docker was not found. Install Docker and make sure it is on the PATH.",
                inner: ex);
        }

        using var _ = process;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync(ct);
        await output;

        if (process.ExitCode != 0)
        {
            var stderr = (await error).Trim();

            // "docker" exists but its daemon isn't running (Docker Desktop not started).
            // The wording differs between Docker versions and platforms, e.g.
            //   Linux:   "Cannot connect to the Docker daemon at unix:///var/run/docker.sock. Is the docker daemon running?"
            //   macOS:   "failed to connect to the docker API at unix:///... check if ... the daemon is running"
            //   Windows: "error during connect: ..."
            string[] daemonDownHints =
            [
                "Cannot connect to the Docker daemon",
                "failed to connect to the docker API",
                "daemon is running",
                "docker daemon running",
                "error during connect",
            ];
            var daemonDown = daemonDownHints.Any(hint => stderr.Contains(hint, StringComparison.OrdinalIgnoreCase));

            throw new MaestroException(
                daemonDown ? ErrorCode.DockerNotAvailable : ErrorCode.SnapserverStartFailed,
                $"docker {string.Join(' ', arguments.Take(2))} failed with exit code {process.ExitCode}: {stderr}");
        }
    }
}
