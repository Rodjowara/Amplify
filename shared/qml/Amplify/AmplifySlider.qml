import QtQuick
import QtQuick.Controls.Basic

// Horizontal or vertical slider in the Amplify style (seek bar, volume, levels, EQ bands).
// All Slider properties (from, to, value, stepSize, orientation...) work as usual.
Slider {
    id: control

    property color fillColor: Theme.colors.primary
    property color trackColor: Theme.colors.disabled

    focusPolicy: Qt.NoFocus

    background: Rectangle {
        x: control.leftPadding + (control.horizontal ? 0 : (control.availableWidth - width) / 2)
        y: control.topPadding + (control.horizontal ? (control.availableHeight - height) / 2 : 0)
        implicitWidth: control.horizontal ? 200 : Theme.size.sliderTrack
        implicitHeight: control.horizontal ? Theme.size.sliderTrack : 200
        width: control.horizontal ? control.availableWidth : implicitWidth
        height: control.horizontal ? implicitHeight : control.availableHeight
        radius: Theme.size.sliderTrack / 2
        color: control.trackColor

        // Filled part: from the start to the handle (bottom-up when vertical).
        Rectangle {
            width: control.horizontal ? control.visualPosition * parent.width : parent.width
            height: control.horizontal ? parent.height : (1 - control.visualPosition) * parent.height
            y: control.horizontal ? 0 : control.visualPosition * parent.height
            radius: parent.radius
            color: control.enabled ? control.fillColor : Theme.colors.disabledText
        }
    }

    handle: Rectangle {
        x: control.leftPadding + (control.horizontal
            ? control.visualPosition * (control.availableWidth - width)
            : (control.availableWidth - width) / 2)
        y: control.topPadding + (control.horizontal
            ? (control.availableHeight - height) / 2
            : control.visualPosition * (control.availableHeight - height))
        implicitWidth: Theme.size.sliderHandle
        implicitHeight: Theme.size.sliderHandle
        radius: width / 2
        color: control.enabled ? Theme.colors.text : Theme.colors.disabledText
    }
}
