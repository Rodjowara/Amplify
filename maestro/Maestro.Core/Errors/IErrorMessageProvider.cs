namespace Maestro.Core.Errors;

/// <summary>
/// Turns an <see cref="ErrorCode"/> into text for the user. One implementation per language,
/// so adding a language later doesn't touch the code that throws or shows errors.
/// </summary>
public interface IErrorMessageProvider
{
    /// <param name="code">What went wrong.</param>
    /// <param name="args">Values for the <c>{name}</c> placeholders, see <see cref="MaestroException.Args"/>.</param>
    string GetMessage(ErrorCode code, IReadOnlyDictionary<string, object?> args);
}
