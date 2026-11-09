using Maestro.Application.Models;
using Maestro.Desktop;
using Maestro.Infrastructure.Services;
using Maestro.Infrastructure.Services.Playback;
using Qt.Quick;

// One snapserver stream for local files; the bootstrap and the source share its config.
var localStream = new SnapcastStreamOptions();

await using var snapserver = new SnapcastBootstrap(localStream);
await snapserver.ExecuteAsync();

// Declared after snapserver so it's disposed first (stops ffmpeg before the container is removed).
await using var localSource = new LocalFileSource(localStream);
PlaybackViewModel.Source = localSource;

// TODO: better way?
var dummyService = new DummyGrpcService("http://localhost:5000");
DummyViewModel.Service = dummyService;

Qml.LoadFromRootModule("Main");
Qml.WaitForExit();
