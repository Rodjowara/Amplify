using Grpc.Core;

namespace Maestro.Core.Errors;

public class MaestroException : Exception
{
    public ErrorCode Code { get; }
    public IReadOnlyDictionary<string, string> Arguments { get; }

    #region Constructors
    public MaestroException(ErrorCode code, object? arguments = null, Exception? inner = null)
        : base(null, inner)
    {
        Code = code;
        Arguments = ToDictionary(arguments);
    }

    public MaestroException(Exception exception)
        : this(ErrorCode.Unknown, new { message = exception.Message }, exception) { }

    public MaestroException(RpcException exception, object? arguments = null)
        : this(ToErrorCode(exception.StatusCode), arguments, exception) { }
    #endregion

    #region Utils
    private static ErrorCode ToErrorCode(StatusCode status) => status switch
    {
        StatusCode.Unavailable => ErrorCode.AMP_Unreachable,
        StatusCode.DeadlineExceeded => ErrorCode.AMP_Timeout,
        _ => ErrorCode.Unknown,
    };

    private static Dictionary<string, string> ToDictionary(object? arguments) =>
        arguments?.GetType().GetProperties()
            .ToDictionary(property => property.Name, property => property.GetValue(arguments)?.ToString() ?? "")
        ?? [];
    #endregion
}
