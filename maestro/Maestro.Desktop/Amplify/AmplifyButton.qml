import QtQuick
import QtQuick.Controls.Basic

// General purpose button. Colors come from the variant but can be overridden per instance,
// and contentItem/background can still be replaced like on any Qt Quick button.
Button {
    id: control

    enum Variant { Primary }
    property int variant: AmplifyButton.Variant.Primary

    property color backgroundColor: Theme.colors.primary
    property color textColor: Theme.colors.primaryText
    property real radius: Theme.radius.normal

    implicitHeight: Theme.size.controlHeight
    font.pixelSize: Theme.fontSize.normal
    hoverEnabled: false
    focusPolicy: Qt.NoFocus

    icon.width: Theme.size.icon
    icon.height: Theme.size.icon
    icon.color: enabled ? textColor : Theme.colors.disabledText

    palette.buttonText: enabled ? textColor : Theme.colors.disabledText

    background: Rectangle {
        radius: control.radius
        color: !control.enabled ? Theme.colors.disabled
             : control.down ? Qt.darker(control.backgroundColor, Theme.pressedDarker)
             : control.backgroundColor
    }
}
