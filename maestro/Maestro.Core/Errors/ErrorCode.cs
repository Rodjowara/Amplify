namespace Maestro.Core.Errors;

public enum ErrorCode
{
    Unknown,

    AMP_InvalidAddress,
    AMP_AlreadyAdded,
    AMP_Unexpected,
    AMP_Unreachable,
    AMP_Timeout,

    FileNotFound,
    FfmpegNotFound,
    SnapserverUnreachable,
    StreamInterrupted,

    SnapserverStartFailed,
}
