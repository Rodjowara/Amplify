import QtQuick
import QtQuick.Controls.Basic
import QtQuick.Layouts
import Amplify

// Shows warnings and errors (not fatal ones, see ErrorDialog). Warnings close themselves after a few
// seconds, errors stay until the user closes them.
// TODO: move to shared/qml/Amplify and the colors to Theme once the amp needs it.
Rectangle {
    id: banner

    // The ErrorViewModel to show.
    required property var errors

    readonly property color errorColor: "#c62828"
    readonly property color warningColor: "#b26a00"
    readonly property int warningTimeout: 5000

    visible: errors.hasError && errors.severity !== "fatal"
    color: errors.severity === "warning" ? warningColor : errorColor
    radius: Theme.radius.normal
    implicitHeight: row.implicitHeight + 2 * Theme.spacing.normal

    // Restarted for every new warning, so each one is visible for the full time.
    function updateTimer() {
        if (visible && errors.severity === "warning")
            dismissTimer.restart()
        else
            dismissTimer.stop()
    }

    onVisibleChanged: updateTimer()

    Timer {
        id: dismissTimer
        interval: banner.warningTimeout
        onTriggered: banner.errors.dismiss()
    }

    RowLayout {
        id: row
        anchors.fill: parent
        anchors.margins: Theme.spacing.normal
        spacing: Theme.spacing.normal

        Label {
            text: banner.errors.message
            color: Theme.colors.text
            font.pixelSize: Theme.fontSize.normal
            wrapMode: Text.WordWrap
            Layout.fillWidth: true
            onTextChanged: banner.updateTimer()
        }

        Button {
            text: "✕"
            flat: true
            focusPolicy: Qt.NoFocus
            palette.buttonText: Theme.colors.text
            font.pixelSize: Theme.fontSize.normal
            Layout.alignment: Qt.AlignTop
            onClicked: banner.errors.dismiss()
        }
    }
}
