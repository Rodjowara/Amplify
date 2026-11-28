using Maestro.Core.Models;
using Maestro.Core.Errors;
using Maestro.Core.Interfaces;
using Maestro.Core.Services;
using Maestro.Desktop;
using Maestro.Desktop.ViewModels;
using Maestro.Core.Services.Playback;
using Microsoft.Extensions.DependencyInjection;
using Qt.Quick;

// SERVICES
var services = new ServiceCollection();
services.AddSingleton<ErrorReporter>();
services.AddSingleton<IAmpManager, AmpManager>();
// TODO(#136): replace the dummy with the amp's status over gRPC; until then every added amp shows as online.
services.AddSingleton<IAmpStatusChecker, DummyAmpStatusChecker>();

services.AddSingleton<LocalFileSource>();

services.AddSingleton<SnapcastStreamOptions>();
services.AddSingleton<SnapcastBootstrap>();

services.AddSingleton<SpotifyConnection>();

await using var provider = services.BuildServiceProvider();
MaestroServices.ServiceProvider = provider;

var errors = provider.GetRequiredService<ErrorReporter>();
TaskScheduler.UnobservedTaskException += (_, args) =>
{
    errors.ShowErrorPage(args.Exception);
    args.SetObserved();
};

// QML
Qml.LoadFromRootModule("Main");
_ = StartSnapserverAsync();
Qml.WaitForExit();

async Task StartSnapserverAsync()
{
    try
    {
        await provider.GetRequiredService<SnapcastBootstrap>().ExecuteAsync();
    }
    catch (Exception exception)
    {
        errors.ShowErrorPage(exception);
    }
}
