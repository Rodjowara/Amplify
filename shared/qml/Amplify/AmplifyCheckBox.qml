import QtQuick
import QtQuick.Controls.Basic
import QtQuick.Controls.impl

CheckBox {
    id: control

    font.pixelSize: Theme.fontSize.normal
    spacing: Theme.spacing.small
    focusPolicy: Qt.NoFocus

    indicator: Rectangle {
        x: control.leftPadding
        y: control.topPadding + (control.availableHeight - height) / 2
        implicitWidth: Theme.size.checkIndicator
        implicitHeight: Theme.size.checkIndicator
        radius: Theme.radius.small
        color: !control.enabled ? Theme.colors.disabled
             : control.checked ? Theme.colors.primary
             : Theme.colors.surface
        border.color: control.checked ? color : Theme.colors.border

        IconImage {
            anchors.fill: parent
            visible: control.checked
            source: Icons.check
            color: control.enabled ? Theme.colors.primaryText : Theme.colors.disabledText
            sourceSize.width: width
            sourceSize.height: height
        }
    }

    contentItem: Text {
        leftPadding: control.indicator.width + control.spacing
        text: control.text
        font: control.font
        color: control.enabled ? Theme.colors.textMuted : Theme.colors.disabledText
        verticalAlignment: Text.AlignVCenter
    }
}
