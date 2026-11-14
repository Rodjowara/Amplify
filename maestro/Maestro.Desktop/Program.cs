using Maestro.Application.Models;
using Maestro.Core.Errors;
using Maestro.Core.Interfaces;
using Maestro.Core.Services;
using Maestro.Desktop;
using Maestro.Infrastructure.Services.Playback;
using Microsoft.Extensions.DependencyInjection;
using Qt.Quick;

var localStream = new SnapcastStreamOptions();

// SNAPCAST PROCESS
// Started below, once the window is up. Declared first so it is disposed last (after playback
// has stopped), which removes the container when the window is closed.
await using var snapserver = new SnapcastBootstrap(localStream);

// SERVICES
var services = new ServiceCollection();
services.AddSingleton<IErrorMessageProvider, EnglishErrorMessages>();
services.AddSingleton<ErrorReporter>();
services.AddSingleton<IErrorReporter>(sp => sp.GetRequiredService<ErrorReporter>());
services.AddSingleton<IAmpManager, AmpManager>();
services.AddSingleton(localStream);
services.AddSingleton<LocalFileSource>();

await using var provider = services.BuildServiceProvider();

// ERRORS
var errors = provider.GetRequiredService<ErrorReporter>();

// Last resort for exceptions nothing else caught. The process ends after UnhandledException,
// so this only gets the error written out.
AppDomain.CurrentDomain.UnhandledException += (_, e) =>
    errors.Report(e.ExceptionObject as Exception ?? new Exception(e.ExceptionObject.ToString()), ErrorSeverity.Fatal);
TaskScheduler.UnobservedTaskException += (_, e) =>
{
    errors.Report(e.Exception);
    e.SetObserved();
};

// VIEWMODELS (QML creates the instances, so they get their services through static properties)
ErrorViewModel.Reporter = errors;

// TEST
var manager = provider.GetRequiredService<IAmpManager>();
manager.Add("http://127.0.0.1:50051");
DummyViewModel.Manager = manager;
DummyViewModel.Errors = errors;

PlaybackViewModel.Source = provider.GetRequiredService<LocalFileSource>();
PlaybackViewModel.Errors = errors;

// QML
// The window is shown first and snapserver starts in the background, so a failure (e.g. Docker
// not running) shows up as a dialog with Retry instead of crashing before there is any window.
Qml.LoadFromRootModule("Main");
errors.Run(() => snapserver.ExecuteAsync(), ErrorSeverity.Fatal, retryable: true);
Qml.WaitForExit();
