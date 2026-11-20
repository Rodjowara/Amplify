using Maestro.Core.Errors;
using Maestro.Desktop.ViewModels;
using Qt.Quick;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Maestro.Desktop;

[QmlElement]
public class ErrorViewModel : BaseViewModel, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly ErrorReporter _reporter = GetRequiredService<ErrorReporter>();

    private MaestroException? _bannerError;
    private MaestroException? _pageError;

    public ErrorViewModel()
    {
        _reporter.Reported += error => { _bannerError = error; Refresh(); };
        _reporter.ErrorPageRequested += error => { _pageError = error; Refresh(); };
    }

    private MaestroException? ShownError => _pageError ?? _bannerError;

    public bool ShowPage => _pageError is not null;
    public bool ShowBanner => _pageError is null && _bannerError is not null;
    public string Code => ShownError?.Code.ToString() ?? "";

    public string Argument(string name) => ShownError?.Arguments.GetValueOrDefault(name) ?? "";

    public void DismissBanner()
    {
        _bannerError = null;
        Refresh();
    }

    private void Refresh()
    {
        OnPropertyChanged(nameof(ShowPage));
        OnPropertyChanged(nameof(ShowBanner));
        OnPropertyChanged(nameof(Code));
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
