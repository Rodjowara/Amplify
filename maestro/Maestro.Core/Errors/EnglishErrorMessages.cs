using System.Text.RegularExpressions;

namespace Maestro.Core.Errors;

/// <summary>
/// English texts for every <see cref="ErrorCode"/>. Each message says what happened and what the user can do.
/// No hard-coding of any exception, port, address, or other variable date here !!!
/// </summary>
public sealed partial class EnglishErrorMessages : IErrorMessageProvider
{
    internal static readonly IReadOnlyDictionary<ErrorCode, string> Templates = new Dictionary<ErrorCode, string>
    {
        [ErrorCode.Unknown] = "Something went wrong. Please try again.",

        [ErrorCode.InvalidAmpAddress] = "\"{address}\" is not a valid amp address. Use the form http://host:port.",
        [ErrorCode.AmpUnreachable] = "Can't reach amp \"{amp}\". Check that it is powered on and on the same network.",
        [ErrorCode.AmpTimeout] = "Amp \"{amp}\" did not answer in time. Check the network connection and try again.",
        [ErrorCode.AmpRejectedRequest] = "Amp \"{amp}\" rejected the request.",

        [ErrorCode.UnsupportedTrackSource] = "This track can't be played as a local file.",
        [ErrorCode.FileNotFound] = "The file \"{path}\" could not be found.",
        [ErrorCode.FfmpegNotFound] = "ffmpeg is not installed. Install it, make sure it is on the PATH, and restart Maestro.",
        [ErrorCode.SnapserverUnreachable] = "Can't connect to the audio server. Wait a moment and try again.",
        [ErrorCode.StreamInterrupted] = "Playback stopped because the connection to the audio server was lost.",

        [ErrorCode.DockerNotAvailable] = "Docker is not available. Install Docker and make sure it is running, then press Retry.",
        [ErrorCode.SnapserverStartFailed] = "The audio server could not be started. Make sure Docker is running, then press Retry.",
    };

    public string GetMessage(ErrorCode code, IReadOnlyDictionary<string, object?> args)
    {
        var template = Templates.GetValueOrDefault(code) ?? Templates[ErrorCode.Unknown];

        // Missing placeholders stay as "{name}" instead of throwing, so a forgotten arg never hides the error.
        return Placeholder().Replace(template, match =>
            args.TryGetValue(match.Groups[1].Value, out var value) && value is not null
                ? value.ToString() ?? match.Value
                : match.Value);
    }

    [GeneratedRegex(@"\{(\w+)\}")]
    private static partial Regex Placeholder();
}
