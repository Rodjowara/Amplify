import QtQuick
import QtQuick.Controls.Basic
import QtQuick.Layouts
import Amplify

MaestroDialog {
    id: root

    required property var vm

    title: qsTr("Add amp")
    acceptText: vm.isAddingAmp ? qsTr("Connecting…") : qsTr("Add")
    acceptEnabled: !vm.isAddingAmp && nameField.text.trim() !== "" && ipField.text.trim() !== ""
    errorText: vm.addAmpErrorCode === "" ? "" : ErrorMessages.text(vm.addAmpErrorCode, name => vm.addAmpErrorArgument(name))

    onAboutToShow: {
        nameField.clear()
        ipField.clear()
        vm.clearAddAmpError()
    }
    onOpened: nameField.forceActiveFocus()
    onAcceptClicked: vm.addAmp(nameField.text, ipField.text)

    Connections {
        target: root.vm
        function onIsAddingAmpChanged() {
            if (root.opened && !root.vm.isAddingAmp && root.vm.addAmpErrorCode === "")
                root.accept()
        }
    }

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
