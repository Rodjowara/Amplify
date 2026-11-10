using Grpc.Core;

namespace Maestro.Core.Errors;

/// <summary>Turns gRPC failures into <see cref="MaestroException"/>s.</summary>
public static class GrpcErrorMapper
{
    /// <param name="ex">The exception thrown by the gRPC client.</param>
    /// <param name="amp">Address of the amp the call went to, used in the user-facing message.</param>
    public static MaestroException Map(RpcException ex, string amp)
    {
        var (code, text) = ex.StatusCode switch
        {
            StatusCode.Unavailable => (ErrorCode.AmpUnreachable, "Amp is unreachable"),
            StatusCode.DeadlineExceeded => (ErrorCode.AmpTimeout, "Amp did not answer in time"),
            StatusCode.InvalidArgument or StatusCode.FailedPrecondition or StatusCode.OutOfRange
                => (ErrorCode.AmpRejectedRequest, "Amp rejected the request"),
            _ => (ErrorCode.Unknown, $"Unexpected gRPC status {ex.StatusCode}"),
        };

        var args = new Dictionary<string, object?>
        {
            ["amp"] = amp,
            ["detail"] = ex.Status.Detail,
        };

        return new MaestroException(code, $"{text} ({amp}): {ex.Status}", args, ex);
    }
}
