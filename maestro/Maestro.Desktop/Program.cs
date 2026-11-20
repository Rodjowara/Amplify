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
await using var snapserver = new SnapcastBootstrap(localStream);

// SERVICES
var services = new ServiceCollection();
services.AddSingleton<ErrorReporter>();
services.AddSingleton<IAmpManager, AmpManager>();
// TODO(#136): replace the dummy with the amp's status over gRPC; until then every added amp shows as online.
services.AddSingleton<IAmpStatusChecker, DummyAmpStatusChecker>();
services.AddSingleton(localStream);
services.AddSingleton<LocalFileSource>();

await using var provider = services.BuildServiceProvider();

var errors = provider.GetRequiredService<ErrorReporter>();
TaskScheduler.UnobservedTaskException += (_, args) =>
{
    errors.ShowErrorPage(args.Exception);
    args.SetObserved();
};
ErrorViewModel.Reporter = errors;

// TEST
var manager = provider.GetRequiredService<IAmpManager>();
manager.Add("http://127.0.0.1:50051");
DummyViewModel.Manager = manager;
DummyViewModel.Errors = errors;

SidebarViewModel.StatusChecker = provider.GetRequiredService<IAmpStatusChecker>();

PlaybackViewModel.Source = provider.GetRequiredService<LocalFileSource>();
PlaybackViewModel.Errors = errors;

// QML
Qml.LoadFromRootModule("Main");
_ = StartSnapserverAsync();
Qml.WaitForExit();

async Task StartSnapserverAsync()
{
    try
    {
        await snapserver.ExecuteAsync();
    }
    catch (Exception exception)
    {
        errors.ShowErrorPage(exception);
    }
}
