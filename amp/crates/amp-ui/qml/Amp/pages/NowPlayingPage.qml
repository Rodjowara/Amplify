import QtQuick
import Amplify
import amp_ui

Item {
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
