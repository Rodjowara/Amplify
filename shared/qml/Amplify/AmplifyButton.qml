import QtQuick
import QtQuick.Controls.Basic

// General purpose button. Colors come from the variant but can be overridden per instance,
// and contentItem/background can still be replaced like on any Qt Quick button.
//   Primary: filled with the primary color.
//   Ghost:   transparent, for icon buttons in toolbars. When checked, the icon turns primary.
Button {
    id: control

    enum Variant { Primary, Ghost }
    property int variant: AmplifyButton.Variant.Primary
    readonly property bool ghost: variant === AmplifyButton.Variant.Ghost

    property color backgroundColor: ghost ? "transparent" : Theme.colors.primary
    property color textColor: ghost ? (checked ? Theme.colors.primary : Theme.colors.text)
                                    : Theme.colors.primaryText
    property real radius: Theme.radius.normal

    implicitHeight: Theme.size.controlHeight
    font.pixelSize: Theme.fontSize.normal
    hoverEnabled: ghost
    focusPolicy: Qt.NoFocus

    icon.width: Theme.size.icon
    icon.height: Theme.size.icon
    icon.color: enabled ? textColor : Theme.colors.disabledText

    palette.buttonText: enabled ? textColor : Theme.colors.disabledText

    background: Rectangle {
        radius: control.radius
        color: !control.enabled ? (control.ghost ? "transparent" : Theme.colors.disabled)
             : control.ghost && (control.down || control.hovered) ? Theme.colors.surfaceHover
             : control.down ? Qt.darker(control.backgroundColor, Theme.pressedDarker)
             : control.backgroundColor
    }
}
