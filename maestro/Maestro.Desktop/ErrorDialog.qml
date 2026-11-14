import QtQuick
import QtQuick.Controls.Basic
import QtQuick.Layouts
import Amplify

// Blocks the app for fatal errors (e.g. snapserver could not be started). The user can only retry
// (if the failed action supports it) or quit.
// TODO: move to shared/qml/Amplify once the amp needs it.
Dialog {
    id: dialog

    // The ErrorViewModel to show.
    required property var errors

    readonly property color errorColor: "#c62828"

    visible: errors.hasError && errors.severity === "fatal"
    modal: true
    closePolicy: Popup.NoAutoClose
    anchors.centerIn: Overlay.overlay
    width: Overlay.overlay ? Overlay.overlay.width - 2 * Theme.spacing.large : implicitWidth
    padding: Theme.spacing.large

    background: Rectangle {
        color: Theme.colors.surface
        radius: Theme.radius.normal
    }

    contentItem: ColumnLayout {
        spacing: Theme.spacing.normal

        Label {
            text: "Maestro can't continue"
            color: Theme.colors.text
            font.pixelSize: Theme.fontSize.large
            wrapMode: Text.WordWrap
            Layout.fillWidth: true
        }

        Label {
            text: dialog.errors.message
            color: Theme.colors.textMuted
            font.pixelSize: Theme.fontSize.normal
            wrapMode: Text.WordWrap
            Layout.fillWidth: true
        }

        RowLayout {
            spacing: Theme.spacing.normal
            Layout.fillWidth: true
            Layout.topMargin: Theme.spacing.normal

            AmplifyButton {
                text: "Retry"
                visible: dialog.errors.canRetry
                Layout.fillWidth: true
                onClicked: dialog.errors.retry()
            }

            AmplifyButton {
                text: "Quit"
                backgroundColor: dialog.errorColor
                Layout.fillWidth: true
                // Ends Qml.WaitForExit() normally, so Program.cs still cleans up (e.g. removes the container).
                onClicked: Qt.quit()
            }
        }
    }
}
