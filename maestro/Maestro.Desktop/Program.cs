using Maestro.Application.Models;
using Maestro.Core.Interfaces;
using Maestro.Core.Services;
using Maestro.Desktop;
using Maestro.Infrastructure.Services.Playback;
using Microsoft.Extensions.DependencyInjection;
using Qt.Quick;

var localStream = new SnapcastStreamOptions();

// SNAPCAST PROCESS
await using var snapserver = new SnapcastBootstrap(localStream);
await snapserver.ExecuteAsync();

// SERVICES
var services = new ServiceCollection();
services.AddSingleton<IAmpManager, AmpManager>();

using var provider = services.BuildServiceProvider();

// TEST
var manager = provider.GetService<IAmpManager>()!;
manager.Add("http://127.0.0.1:50051");
DummyViewModel.Manager = manager;

await using var localSource = new LocalFileSource(localStream);
PlaybackViewModel.Source = localSource;

// QML
Qml.LoadFromRootModule("Main");
Qml.WaitForExit();
