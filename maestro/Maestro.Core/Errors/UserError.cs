namespace Maestro.Core.Errors;

/// <summary>An error as the user sees it: the message is already translated, technical details are gone.</summary>
/// <param name="Code">What went wrong</param>
/// <param name="Severity">How it is shown</param>
/// <param name="Message">Text for the user</param>
/// <param name="At">When it was reported</param>
/// <param name="Retry">Runs the failed action again, or null if it can't be retried</param>
public sealed record UserError(
    ErrorCode Code,
    ErrorSeverity Severity,
    string Message,
    DateTimeOffset At,
    Func<Task>? Retry = null);
