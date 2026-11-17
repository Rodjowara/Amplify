namespace Maestro.Core.Errors;

public enum ErrorCode
{
    Unknown,

    InvalidAmpAddress,
    AmpUnreachable,
    AmpTimeout,
    AmpRejectedRequest,

    FileNotFound,
    FfmpegNotFound,
    SnapserverUnreachable,
    StreamInterrupted,

    SnapserverStartFailed,
}
