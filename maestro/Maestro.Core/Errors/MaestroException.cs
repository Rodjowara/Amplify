using Grpc.Core;

namespace Maestro.Core.Errors;

public class MaestroException : Exception
{
    public ErrorCode Code { get; }
    public IReadOnlyDictionary<string, string> Arguments { get; }

    public override string Message =>
        $"{Code}: {string.Join(", ", Arguments.Select(argument => $"{argument.Key}={argument.Value}"))}";

    public MaestroException(ErrorCode code, object? arguments = null, Exception? inner = null)
        : base(null, inner)
    {
        Code = code;
        Arguments = ToDictionary(arguments);
    }

    public MaestroException(Exception exception)
        : this(ErrorCode.Unknown, new { message = exception.Message }, exception)
    {
    }

    public MaestroException(RpcException exception, object? arguments)
        : this(ToErrorCode(exception.StatusCode), arguments, exception)
    {
    }

    private static ErrorCode ToErrorCode(StatusCode status) => status switch
    {
        StatusCode.Unavailable => ErrorCode.AmpUnreachable,
        StatusCode.DeadlineExceeded => ErrorCode.AmpTimeout,
        StatusCode.InvalidArgument or StatusCode.FailedPrecondition => ErrorCode.AmpRejectedRequest,
        _ => ErrorCode.Unknown,
    };

    private static Dictionary<string, string> ToDictionary(object? arguments) =>
        arguments?.GetType().GetProperties()
            .ToDictionary(property => property.Name, property => property.GetValue(arguments)?.ToString() ?? "")
        ?? [];
}
