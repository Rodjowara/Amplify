import QtQuick
import Amplify
import amp_ui

Window {
    width: 320; height: 200; visible: true
    title: "Amp"
    color: Theme.colors.background

    Column {
        anchors.centerIn: parent
        spacing: Theme.spacing.normal

        Text {
            text: Greeter.message
            color: Theme.colors.text
            font.pixelSize: Theme.fontSize.normal
            anchors.horizontalCenter: parent.horizontalCenter
        }

        AmplifyPlayPauseButton {
            anchors.horizontalCenter: parent.horizontalCenter
        }
    }
}