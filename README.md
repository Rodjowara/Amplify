**Project structure**<br>
Maestro -> code for the Maestro part of the project<br>
Amp -> code for the Amp part of the project<br>
Shared -> proto file and all other files that need to be shared across subprojects<br>

**Amp subproject, folder explanation**<br>
crates/amp-app -> main.rs, startup, wiring, tokio runtime<br>
crates/amp-core -> shared state of the system (eg. audio volume is needed for both the screen and the DSP chain)<br>
crates/amp-control -> gRPC server<br>
crates/amp-audio -> DSP chain and Snapcast wrapper<br>
crates/amp-ui -> Qt Bridge<br>
crates/amp-ui/qml -> QML files<br>
