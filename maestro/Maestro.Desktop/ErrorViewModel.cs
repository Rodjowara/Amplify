using Maestro.Core.Errors;
using Qt.Quick;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Maestro.Desktop;

/// <summary>
/// The error currently shown to the user, for ErrorBanner.qml and ErrorDialog.qml.
/// All properties are read from one immutable <see cref="UserError"/>, so QML always sees a consistent error
/// even though errors are reported from background threads. (Qt Bridge delivers PropertyChanged
/// on the UI thread, so raising it from any thread is fine.)
/// </summary>
[QmlElement]
public class ErrorViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    // Set in Program.cs (same static pattern as the other ViewModels, since QML creates the instance).
    [Qt.Ignore]
    public static ErrorReporter Reporter { get; set; } = null!;

    private UserError? _error;

    public ErrorViewModel()
    {
        Reporter.CurrentChanged += OnCurrentChanged;
        // An error may have been reported before QML created this instance (e.g. during startup).
        OnCurrentChanged(Reporter.Current);
    }

    public bool HasError => _error is not null;

    public string Message => _error?.Message ?? "";

    /// <summary>"warning", "error", "fatal", or "" if there is no error.</summary>
    public string Severity => _error?.Severity switch
    {
        ErrorSeverity.Warning => "warning",
        ErrorSeverity.Error => "error",
        ErrorSeverity.Fatal => "fatal",
        _ => "",
    };

    public bool CanRetry => _error?.Retry is not null;

    /// <summary>Called from QML. Hides the current error.</summary>
    public void Dismiss()
    {
        if (_error is { } error)
            Reporter.Dismiss(error);
    }

    /// <summary>Called from QML. Hides the current error and runs the failed action again.</summary>
    public void Retry()
    {
        if (_error is not { Retry: { } retry } error)
            return;

        Reporter.Dismiss(error);
        // A new failure is reported again by the retry action itself; Run only guards against it throwing.
        Reporter.Run(retry, error.Severity);
    }

    private void OnCurrentChanged(UserError? error)
    {
        _error = error;
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(Message));
        OnPropertyChanged(nameof(Severity));
        OnPropertyChanged(nameof(CanRetry));
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
