using System.Diagnostics;

namespace Maestro.Desktop;

public class SnapcastBootstrap : IAsyncDisposable
{
    private readonly string _name = "maestro-snapserver";
    private readonly string _command = string.Join(" && ",
          // install snappserver
          "apk add --no-cache snapcast-server",
          // snapserver config
          "printf '[stream]\\nsource = tcp://0.0.0.0:4953?name=default&mode=server&sampleformat=48000:16:2&codec=flac\\n' > /etc/snapserver.conf",
          // start service
          "exec snapserver");

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        // NOTE: docker cleanup
        await ExecuteDockerAsync(["rm", "-f", _name], CancellationToken.None);

        var arguments = new List<string>();

        arguments.AddRange(["run", "-d"]);
        arguments.AddRange(["--name", _name]);

        // NOTE: audio input (port 4953)
        arguments.AddRange(["-p", "127.0.0.1:4953:4953"]);
        // NOTE: audio output (port 1704)
        arguments.AddRange(["-p", "1704:1704",]);

        arguments.Add("alpine:3.21");
        arguments.AddRange(["sh", "-c", _command]);

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
