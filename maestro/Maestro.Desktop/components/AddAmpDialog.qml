import QtQuick
import QtQuick.Controls.Basic
import QtQuick.Layouts
import Amplify

MaestroDialog {
    id: root

    required property var vm

    title: qsTr("Add amp")
    acceptText: qsTr("Add")
    acceptEnabled: nameField.text.trim() !== "" && ipField.text.trim() !== ""
    errorText: vm.addAmpErrorCode === "" ? "" : ErrorMessages.text(vm.addAmpErrorCode, name => vm.addAmpErrorArgument(name))

    onAboutToShow: {
        nameField.clear()
        ipField.clear()
        vm.clearAddAmpError()
    }
    onOpened: nameField.forceActiveFocus()
    onAcceptClicked: if (vm.addAmp(nameField.text, ipField.text)) accept()

    ColumnLayout {
        anchors.fill: parent
        spacing: Theme.spacing.small

        Label {
            text: qsTr("Name")
            color: Theme.colors.textMuted
            font.pixelSize: Theme.fontSize.small
        }

        TextField {
            id: nameField
            placeholderText: qsTr("Stage 1 Left")
            Layout.fillWidth: true
            onAccepted: ipField.forceActiveFocus()
        }

        Label {
            text: qsTr("IP address")
            color: Theme.colors.textMuted
            font.pixelSize: Theme.fontSize.small
            Layout.topMargin: Theme.spacing.normal
        }

        TextField {
            id: ipField
            placeholderText: "192.168.1.21"
            Layout.fillWidth: true
            onAccepted: if (root.acceptEnabled) root.acceptClicked()
        }
    }
}
