import QtQuick
import QtQuick.Controls.Basic
import QtQuick.Layouts
import Amplify

ApplicationWindow {
    id: win
    visible: true
    title: "Qt Bridge for C#"
    width: 365; height: 510
    color: Theme.colors.background

    DummyViewModel {
        id: dummy
    }

    ColumnLayout {
        anchors.fill: parent
        anchors.margins: Theme.spacing.large
        spacing: Theme.spacing.large

        Label {
            text: "Hello, World!"
            font.pixelSize: Theme.fontSize.title
            color: Theme.colors.text
            horizontalAlignment: Text.AlignHCenter
            Layout.alignment: Qt.AlignHCenter
        }

        ColumnLayout {
            Layout.fillWidth: true
            spacing: Theme.spacing.small
            Label {
                text: "Welcome to Qt Bridge for C#"
                Layout.fillWidth: true
                horizontalAlignment: Text.AlignHCenter
                font.pixelSize: Theme.fontSize.large
                color: Theme.colors.textMuted
            }

            Label {
                text: "QML Multi-language App UI"
                Layout.fillWidth: true
                horizontalAlignment: Text.AlignHCenter
                font.pixelSize: Theme.fontSize.large
                color: Theme.colors.text
                wrapMode: Text.WordWrap
            }
        }

        AmplifyButton {
            text: dummy.clicks === 0 ? "Click me" : "Clicked " + dummy.clicks + " times"
            Layout.fillWidth: true
            onClicked: dummy.clicks += 1
        }

        AmplifyButton {
            text: "Ping"
            Layout.fillWidth: true
            onClicked: dummy.ping()
        }

        AmplifyPlayPauseButton {
            Layout.alignment: Qt.AlignHCenter
        }

        Label {
            text: dummy.pingResponse
            color: Theme.colors.text
            Layout.fillWidth: true
            horizontalAlignment: Text.AlignHCenter
            wrapMode: Text.WordWrap
        }
    }
}
