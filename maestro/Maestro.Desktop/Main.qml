import QtQuick
import QtQuick.Controls.Basic
import QtQuick.Layouts
import Amplify

ApplicationWindow {
    id: win
    visible: true
    title: "Maestro"
    width: 365; height: 510
    color: Theme.colors.background

    DummyViewModel {
        id: dummy
    }

    PlaybackViewModel {
        id: playback
    }

    ErrorViewModel {
        id: errorModel
    }

    ErrorDialog {
        errors: errorModel
    }

    ColumnLayout {
        anchors.fill: parent
        anchors.margins: Theme.spacing.large
        spacing: Theme.spacing.large

        Label {
            text: "Maestro"
            font.pixelSize: Theme.fontSize.title
            color: Theme.colors.text
            Layout.alignment: Qt.AlignHCenter
        }

        ColumnLayout {
            Layout.alignment: Qt.AlignHCenter
            spacing: Theme.spacing.small

            AmplifyPlayPauseButton {
                playing: dummy.playing
                onPlayingChanged: dummy.playing = playing
                Layout.alignment: Qt.AlignHCenter
            }

            Label {
                text: dummy.playing ? "Playing" : "Paused"
                font.pixelSize: Theme.fontSize.small
                color: Theme.colors.textMuted
                Layout.alignment: Qt.AlignHCenter
            }
        }

        Item { Layout.fillHeight: true }

        AmplifyButton {
            text: "Send gRPC message"
            Layout.fillWidth: true
            onClicked: dummy.ping()
        }

        Label {
            text: dummy.pingResponse !== "" ? dummy.pingResponse : "No response yet"
            color: dummy.pingResponse !== "" ? Theme.colors.text : Theme.colors.textMuted
            font.pixelSize: Theme.fontSize.normal
            Layout.fillWidth: true
            horizontalAlignment: Text.AlignHCenter
            wrapMode: Text.WordWrap
        }

        // Debug: stream a local audio file to snapserver
        TextField {
            id: trackPath
            placeholderText: "Path to a local audio file"
            Layout.fillWidth: true
        }

        Button {
            text: "Play"
            Layout.fillWidth: true
            onClicked: playback.play(trackPath.text)
            background: Rectangle {
                radius: 12
                border.width: 0
                color: '#f81414'
            }
        }

        Label {
            text: playback.status
            color: Theme.colors.text
            Layout.fillWidth: true
            horizontalAlignment: Text.AlignHCenter
            wrapMode: Text.WordWrap
        }

        ErrorBanner {
            errors: errorModel
            Layout.fillWidth: true
        }
    }
}
