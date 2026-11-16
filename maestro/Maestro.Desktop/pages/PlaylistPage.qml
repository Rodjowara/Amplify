import QtQuick
import QtQuick.Controls.Basic
import QtQuick.Layouts
import Amplify

// The playlist screen isn't built yet; for now it holds the debug tools
// (gRPC ping and playing a local file). `dummy` is a DummyViewModel, `playback` a PlaybackViewModel.
Item {
    id: root

    required property var dummy
    required property var playback

    ColumnLayout {
        anchors.centerIn: parent
        width: Math.min(parent.width - 2 * Theme.spacing.large, 420)
        spacing: Theme.spacing.normal

        Label {
            text: qsTr("Playlist")
            color: Theme.colors.text
            font.pixelSize: Theme.fontSize.title
            Layout.alignment: Qt.AlignHCenter
            Layout.bottomMargin: Theme.spacing.large
        }

        Label {
            text: qsTr("Debug")
            color: Theme.colors.textSubtle
            font.pixelSize: Theme.fontSize.small
            font.capitalization: Font.AllUppercase
        }

        AmplifyButton {
            text: qsTr("Send gRPC message")
            Layout.fillWidth: true
            onClicked: root.dummy.ping()
        }

        Label {
            text: root.dummy.pingResponse !== "" ? root.dummy.pingResponse : qsTr("No response yet")
            color: root.dummy.pingResponse !== "" ? Theme.colors.text : Theme.colors.textSubtle
            font.pixelSize: Theme.fontSize.normal
            horizontalAlignment: Text.AlignHCenter
            wrapMode: Text.WordWrap
            Layout.fillWidth: true
        }

        TextField {
            id: trackPath
            placeholderText: qsTr("Path to a local audio file")
            Layout.fillWidth: true
            Layout.topMargin: Theme.spacing.large
            onAccepted: if (text.trim() !== "") root.playback.play(text)
        }

        AmplifyButton {
            text: qsTr("Play file")
            enabled: trackPath.text.trim() !== ""
            Layout.fillWidth: true
            onClicked: root.playback.play(trackPath.text)
        }

        Label {
            text: root.playback.status
            color: Theme.colors.textMuted
            horizontalAlignment: Text.AlignHCenter
            wrapMode: Text.WordWrap
            Layout.fillWidth: true
        }
    }
}
