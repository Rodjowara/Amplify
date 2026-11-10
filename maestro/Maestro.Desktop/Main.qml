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

    AmpViewModel {
        id: amp
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

        RowLayout {
            Layout.fillWidth: true
            spacing: Theme.spacing.small

            TextField {
                id: ampAddress
                text: "127.0.0.1:50051"
                placeholderText: "Amp address (ip:port)"
                Layout.fillWidth: true
                onAccepted: amp.connect(text)
            }

            AmplifyButton {
                text: "Connect"
                onClicked: amp.connect(ampAddress.text)
            }
        }

        Label {
            text: amp.status !== "" ? amp.status : "No amp connected"
            color: amp.status !== "" ? Theme.colors.text : Theme.colors.textMuted
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
            color: '#f81414'
            Layout.fillWidth: true
            horizontalAlignment: Text.AlignHCenter
            wrapMode: Text.WordWrap
        }
    }
}
