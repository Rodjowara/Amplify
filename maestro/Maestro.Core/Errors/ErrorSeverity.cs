namespace Maestro.Core.Errors;

/// <summary>Defines the severity of an error.</summary>
public enum ErrorSeverity
{
    // Something went wrong but the app is fine
    Warning,
    // An action the user started failed
    Error,
    // The app cannot continue
    Fatal,
}
