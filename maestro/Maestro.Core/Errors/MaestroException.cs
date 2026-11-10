namespace Maestro.Core.Errors;

/// <summary>
/// An expected failure that the user can be told about. Core code wraps third-party exceptions
/// (gRPC, sockets, processes) in this type and keeps the original as <see cref="Exception.InnerException"/>.
/// </summary>
public class MaestroException : Exception
{
    private static readonly IReadOnlyDictionary<string, object?> NoArgs = new Dictionary<string, object?>();

    public ErrorCode Code { get; }

    /// <summary>Values for the user-facing message</summary>
    public IReadOnlyDictionary<string, object?> Args { get; }

    /// <param name="code">What went wrong, used to pick the user-facing message.</param>
    /// <param name="technicalMessage">English text for logs. May contain details, is never shown to the user.</param>
    /// <param name="args">Values for placeholders in the user-facing message.</param>
    /// <param name="inner">The exception that caused this one.</param>
    public MaestroException(
        ErrorCode code,
        string technicalMessage,
        IReadOnlyDictionary<string, object?>? args = null,
        Exception? inner = null)
        : base(technicalMessage, inner)
    {
        Code = code;
        Args = args ?? NoArgs;
    }

    /// <summary>Shorthand for a single placeholder value.</summary>
    public static IReadOnlyDictionary<string, object?> Arg(string key, object? value)
        => new Dictionary<string, object?> { [key] = value };
}
