import QtQuick
import QtQuick.Controls.Basic
import QtQuick.Controls.impl
import QtQuick.Layouts

// Navigation item with an icon and a label, highlighted when `active`.
// Set iconOnly for an icon rail (e.g. the amp panel); the label then shows as a tooltip.
ItemDelegate {
    id: control

    property bool active: false
    property bool iconOnly: false

    implicitHeight: Theme.size.compactControlHeight
    implicitWidth: iconOnly ? implicitHeight : 200
    focusPolicy: Qt.NoFocus

    icon.width: Theme.size.icon
    icon.height: Theme.size.icon
    icon.color: active ? Theme.colors.primary : Theme.colors.text

    ToolTip.visible: iconOnly && hovered
    ToolTip.text: text

    background: Rectangle {
        radius: Theme.radius.small
        color: control.active ? Theme.colors.primarySubtle
             : control.hovered ? Theme.colors.surfaceHover
             : "transparent"
    }

    contentItem: RowLayout {
        spacing: Theme.spacing.normal

        IconImage {
            source: control.icon.source
            color: control.icon.color
            sourceSize.width: control.icon.width
            sourceSize.height: control.icon.height
            Layout.alignment: control.iconOnly ? Qt.AlignHCenter : Qt.AlignVCenter
        }

        Label {
            visible: !control.iconOnly
            text: control.text
            color: control.active ? Theme.colors.primary : Theme.colors.text
            font.pixelSize: Theme.fontSize.normal
            elide: Text.ElideRight
            Layout.fillWidth: true
        }
    }
}
