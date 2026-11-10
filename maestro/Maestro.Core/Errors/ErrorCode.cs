namespace Maestro.Core.Errors;

/// <summary>
/// Definitions for identifiers of expected failures.
/// The UI looks up the text for the user from this code.
/// This way, the wording (and later the language) is independent of the exception message.
/// </summary>
public enum ErrorCode
{
    Unknown,

    // Coming from Amp / gRPC
    InvalidAmpAddress,
    AmpUnreachable,
    AmpTimeout,
    AmpRejectedRequest,

    // Playback
    UnsupportedTrackSource,
    FileNotFound,
    FfmpegNotFound,
    SnapserverUnreachable,
    StreamInterrupted,

    // Startup / environment
    DockerNotAvailable,
    SnapserverStartFailed,
}
