import QtQuick
import QtQuick.Controls.Basic
import QtQuick.Layouts
import Amplify

Item {
    id: root

    required property var errors

    readonly property color errorColor: "#c62828"
    readonly property int pageContentMaxWidth: 420

    function message() {
        switch (errors.code) {
        case "InvalidAmpAddress": return qsTr("\"%1\" is not a valid amp address.").arg(errors.argument("address"))
        case "AmpUnreachable": return qsTr("Can't reach amp %1. Check that it is on and in the same network.").arg(errors.argument("amp"))
        case "AmpTimeout": return qsTr("Amp %1 did not answer in time.").arg(errors.argument("amp"))
        case "AmpRejectedRequest": return qsTr("Amp %1 rejected the request.").arg(errors.argument("amp"))
        case "FileNotFound": return qsTr("The file \"%1\" could not be found.").arg(errors.argument("path"))
        case "FfmpegNotFound": return qsTr("ffmpeg is not installed. Install it and restart Maestro.")
        case "SnapserverUnreachable": return qsTr("Can't connect to the audio server.")
        case "StreamInterrupted": return qsTr("Playback stopped because the connection to the audio server was lost.")
        case "SnapserverStartFailed": return qsTr("Couldn't start the audio server. Make sure Docker is installed and running.")
        default: return qsTr("Something went wrong.")
        }
    }

    Rectangle {
        visible: root.errors.showBanner
        color: root.errorColor
        radius: Theme.radius.normal
        anchors.left: parent.left
        anchors.right: parent.right
        anchors.bottom: parent.bottom
        anchors.margins: Theme.spacing.normal
        implicitHeight: bannerContent.implicitHeight + 2 * Theme.spacing.normal

        RowLayout {
            id: bannerContent
            anchors.fill: parent
            anchors.margins: Theme.spacing.normal
            spacing: Theme.spacing.normal

            Label {
                text: root.message()
                color: Theme.colors.text
                font.pixelSize: Theme.fontSize.normal
                wrapMode: Text.WordWrap
                Layout.fillWidth: true
            }

            Button {
                text: "✕"
                flat: true
                focusPolicy: Qt.NoFocus
                palette.buttonText: Theme.colors.text
                onClicked: root.errors.dismissBanner()
            }
        }
    }

    Rectangle {
        visible: root.errors.showPage
        color: Theme.colors.background
        anchors.fill: parent

        MouseArea {
            anchors.fill: parent
        }

        ColumnLayout {
            anchors.centerIn: parent
            width: Math.min(parent.width - 2 * Theme.spacing.large, root.pageContentMaxWidth)
            spacing: Theme.spacing.large

            Label {
                text: root.message()
                color: Theme.colors.text
                font.pixelSize: Theme.fontSize.large
                horizontalAlignment: Text.AlignHCenter
                wrapMode: Text.WordWrap
                Layout.fillWidth: true
            }

            AmplifyButton {
                text: qsTr("Quit")
                backgroundColor: root.errorColor
                Layout.fillWidth: true
                onClicked: Qt.quit()
            }
        }
    }
}
