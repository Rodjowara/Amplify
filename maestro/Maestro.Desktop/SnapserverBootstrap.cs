using System.Diagnostics;
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
        // NOTE: audio output (snapcast clients)
        arguments.AddRange(["-p", $"{stream.ClientPort}:1704"]);

        arguments.Add("alpine:3.21");
        arguments.AddRange(["sh", "-c", Command]);

        await ExecuteDockerAsync(arguments, ct);
    }

    public async ValueTask DisposeAsync()
    {
        await ExecuteDockerAsync(["rm", "-f", _name], CancellationToken.None);
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

        using var process = Process.Start(info)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync(ct);
        await output;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(await error);
        }
    }
}
