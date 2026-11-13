import QtQuick
import QtQuick.Controls.Basic
import QtQuick.Layouts
import Amplify

// Left sidebar: logo, navigation to Playlist and Settings, and the list of amps.
// Clicking an amp opens its Tuning page. `vm` is a SidebarViewModel.
Rectangle {
    id: root

    required property var vm

    signal addAmpRequested()

    implicitWidth: 240
    color: Theme.colors.surface

    ColumnLayout {
        anchors.fill: parent
        anchors.margins: Theme.spacing.normal
        spacing: Theme.spacing.small

        Label {
            text: "Maestro"
            color: Theme.colors.text
            font.pixelSize: Theme.fontSize.large
            font.bold: true
            Layout.bottomMargin: Theme.spacing.normal
        }

        AmplifyNavButton {
            text: qsTr("Playlist")
            icon.source: Icons.playlist
            active: root.vm.currentPage === "playlist"
            Layout.fillWidth: true
            onClicked: root.vm.navigate("playlist")
        }

        AmplifyNavButton {
            text: qsTr("Settings")
            icon.source: Icons.settings
            active: root.vm.currentPage === "settings"
            Layout.fillWidth: true
            onClicked: root.vm.navigate("settings")
        }

        RowLayout {
            Layout.fillWidth: true
            Layout.topMargin: Theme.spacing.large

            Label {
                text: qsTr("Amps")
                color: Theme.colors.textSubtle
                font.pixelSize: Theme.fontSize.small
                font.capitalization: Font.AllUppercase
                Layout.fillWidth: true
            }

            AmplifyButton {
                variant: AmplifyButton.Variant.Ghost
                display: AbstractButton.IconOnly
                text: qsTr("Add amp")
                icon.source: Icons.plus
                implicitWidth: Theme.size.compactControlHeight
                implicitHeight: Theme.size.compactControlHeight
                radius: Theme.radius.small
                ToolTip.visible: hovered
                ToolTip.text: text
                onClicked: root.addAmpRequested()
            }
        }

        ListView {
            clip: true
            spacing: 2
            model: root.vm.amps
            Layout.fillWidth: true
            Layout.fillHeight: true

            delegate: AmpListItem {
                required property var modelData

                width: ListView.view.width
                name: modelData.name
                ip: modelData.ip
                online: modelData.online
                selected: root.vm.currentPage === "tuning" && root.vm.selectedAmpIp === modelData.ip
                onClicked: root.vm.selectAmp(modelData.ip)
            }
        }
    }
}
