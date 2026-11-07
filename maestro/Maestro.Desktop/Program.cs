using Maestro.Core.Interfaces;
using Maestro.Core.Services;
using Maestro.Desktop;
using Microsoft.Extensions.DependencyInjection;
using Qt.Quick;

// SNAPCAST PROCESS
await using var snapserver = new SnapcastBootstrap();
await snapserver.ExecuteAsync();

// SERVICES
var services = new ServiceCollection();
services.AddSingleton<IAmpManager, AmpManager>();

using var provider = services.BuildServiceProvider();

// TEST
var manager = provider.GetService<IAmpManager>()!;
manager.Add("http://127.0.0.1:50051");
DummyViewModel.Manager = manager;

// QML
Qml.LoadFromRootModule("Main");
Qml.WaitForExit();
