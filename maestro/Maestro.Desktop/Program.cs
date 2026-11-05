using Maestro.Desktop;
using Maestro.Infrastructure.Services;
using Qt.Quick;

await using var snapserver = new SnapcastBootstrap();
await snapserver.ExecuteAsync();

// TODO: better way?
var dummyService = new DummyGrpcService("http://localhost:5000");
DummyViewModel.Service = dummyService;

Qml.LoadFromRootModule("Main");
Qml.WaitForExit();
