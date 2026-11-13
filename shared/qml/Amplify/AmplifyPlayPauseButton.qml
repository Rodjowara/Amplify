import QtQuick
import QtQuick.Controls.Basic

// Round button that shows play or pause. By default it flips `playing` when clicked.
// Set toggleOnClick to false when `playing` is bound to a view model, so the click
// doesn't break the binding; then handle onClicked instead.
AmplifyButton {
    id: control

    property bool playing: false
    property bool toggleOnClick: true

    implicitWidth: implicitHeight
    radius: Theme.radius.round
    display: AbstractButton.IconOnly

    text: playing ? qsTr("Pause") : qsTr("Play")
    icon.source: playing ? Icons.pause : Icons.play

    onClicked: if (toggleOnClick) playing = !playing
}
