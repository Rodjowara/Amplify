import QtQuick
import QtQuick.Controls.Basic

// Round button that shows play or pause. It only flips `playing` when clicked;
// the parent decides what actually happens (onPlayingChanged, onClicked).
AmplifyButton {
    id: control

    property bool playing: false

    implicitWidth: implicitHeight
    radius: Theme.radius.round
    display: AbstractButton.IconOnly

    text: playing ? qsTr("Pause") : qsTr("Play")
    icon.source: playing ? Icons.pause : Icons.play

    onClicked: playing = !playing
}
