namespace Maestro.Core.Errors;

public enum ErrorCode
{
    Unknown,

    InvalidAmpAddress,
    AmpAlreadyAdded,
    AmpUnreachable,
    AmpTimeout,
    AmpRejectedRequest,

    FileNotFound,
    FfmpegNotFound,
    SnapserverUnreachable,
    StreamInterrupted,

    SnapserverStartFailed,
}
