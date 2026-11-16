import QtQuick
import QtQuick.Controls.Basic
import QtQuick.Layouts
import Amplify

// One amp in the sidebar: online dot, name and IP address.
ItemDelegate {
    id: control

    property string name: ""
    property string ip: ""
    property bool online: false
    property bool selected: false

    implicitHeight: Theme.size.controlHeight
    focusPolicy: Qt.NoFocus

    background: Rectangle {
        radius: Theme.radius.small
        color: control.selected ? Theme.colors.primarySubtle
             : control.hovered ? Theme.colors.surfaceHover
             : "transparent"
    }

    contentItem: RowLayout {
        spacing: Theme.spacing.normal

        AmplifyStatusDot {
            online: control.online
        }

        ColumnLayout {
            spacing: 0
            Layout.fillWidth: true

            Label {
                text: control.name
                color: control.selected ? Theme.colors.primary : Theme.colors.text
                font.pixelSize: Theme.fontSize.normal
                elide: Text.ElideRight
                Layout.fillWidth: true
            }

            Label {
                text: control.online ? control.ip : control.ip + " · " + qsTr("offline")
                color: Theme.colors.textSubtle
                font.pixelSize: Theme.fontSize.small
                elide: Text.ElideRight
                Layout.fillWidth: true
            }
        }
    }
}
