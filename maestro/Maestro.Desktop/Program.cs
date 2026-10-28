using Maestro.Desktop;
using Maestro.Infrastructure.Services;
using Qt.Quick;

// TODO: better way?
var dummyService = new DummyGrpcService("http://localhost:5000");
DummyViewModel.Service = dummyService;

Qml.LoadFromRootModule("Main");
Qml.WaitForExit();
