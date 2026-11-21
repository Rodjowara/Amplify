import QtQuick
import QtQuick.Controls.Basic
import QtQuick.Layouts

Item {
    id: root

    property int orientation: Qt.Horizontal
    property alias from: slider.from
    property alias to: slider.to
    property alias value: slider.value
    property alias stepSize: slider.stepSize
    readonly property alias pressed: slider.pressed
    readonly property bool horizontal: orientation === Qt.Horizontal

    property string label: ""
    property string unit: ""
    property string valueText: Math.round(value) + unit
    property bool showControls: label !== ""

    property color fillColor: Theme.colors.primary
    property color trackColor: Theme.colors.disabled

    readonly property int defaultStepCount: 10

    signal moved()

    function adjustBy(direction) {
        const delta = slider.stepSize > 0
            ? slider.stepSize
            : (slider.to - slider.from) / defaultStepCount
        const nextValue = Math.min(slider.to, Math.max(slider.from, slider.value + direction * delta))
        if (nextValue === slider.value)
            return
        slider.value = nextValue
        root.moved()
    }

    implicitWidth: layout.implicitWidth
    implicitHeight: layout.implicitHeight

    GridLayout {
        id: layout
        anchors.fill: parent
        columns: root.horizontal ? 3 : 1
        rowSpacing: root.showControls ? Theme.spacing.small : 0
        columnSpacing: root.showControls ? Theme.spacing.small : 0

        Text {
            visible: root.showControls && root.label !== ""
            text: root.label
            color: root.enabled ? Theme.colors.textSubtle : Theme.colors.disabledText
            font.pixelSize: Theme.fontSize.small
            font.capitalization: root.horizontal ? Font.AllUppercase : Font.MixedCase
            Layout.row: root.horizontal ? 0 : 4
            Layout.column: 0
            Layout.columnSpan: root.horizontal ? 2 : 1
            Layout.alignment: root.horizontal ? (Qt.AlignLeft | Qt.AlignVCenter) : Qt.AlignHCenter
        }

        Text {
            visible: root.showControls
            text: root.valueText
            color: root.enabled ? Theme.colors.textMuted : Theme.colors.disabledText
            font.pixelSize: Theme.fontSize.small
            Layout.row: 0
            Layout.column: root.horizontal ? 2 : 0
            Layout.alignment: root.horizontal ? (Qt.AlignRight | Qt.AlignVCenter) : Qt.AlignHCenter
        }

        AmplifyButton {
            visible: root.showControls
            variant: AmplifyButton.Variant.Ghost
            text: "-"
            textColor: Theme.colors.textMuted
            padding: 0
            implicitWidth: Theme.size.sliderStepButton
            implicitHeight: Theme.size.sliderStepButton
            radius: Theme.radius.round
            enabled: root.enabled && slider.value > slider.from
            Layout.row: root.horizontal ? 1 : 3
            Layout.column: 0
            Layout.alignment: Qt.AlignCenter
            onClicked: root.adjustBy(-1)
        }

        Slider {
            id: slider
            orientation: root.orientation
            focusPolicy: Qt.NoFocus
            Layout.row: root.horizontal ? 1 : 2
            Layout.column: root.horizontal ? 1 : 0
            Layout.fillWidth: root.horizontal
            Layout.fillHeight: !root.horizontal
            Layout.alignment: Qt.AlignCenter
            onMoved: root.moved()

            background: Rectangle {
                x: slider.leftPadding + (slider.horizontal ? 0 : (slider.availableWidth - width) / 2)
                y: slider.topPadding + (slider.horizontal ? (slider.availableHeight - height) / 2 : 0)
                implicitWidth: slider.horizontal ? Theme.size.sliderLength : Theme.size.sliderTrack
                implicitHeight: slider.horizontal ? Theme.size.sliderTrack : Theme.size.sliderLength
                width: slider.horizontal ? slider.availableWidth : implicitWidth
                height: slider.horizontal ? implicitHeight : slider.availableHeight
                radius: Theme.size.sliderTrack / 2
                color: root.trackColor

                Rectangle {
                    width: slider.horizontal ? slider.visualPosition * parent.width : parent.width
                    height: slider.horizontal ? parent.height : (1 - slider.visualPosition) * parent.height
                    y: slider.horizontal ? 0 : slider.visualPosition * parent.height
                    radius: parent.radius
                    color: slider.enabled ? root.fillColor : Theme.colors.disabledText
                }
            }

            handle: Rectangle {
                x: slider.leftPadding + (slider.horizontal
                    ? slider.visualPosition * (slider.availableWidth - width)
                    : (slider.availableWidth - width) / 2)
                y: slider.topPadding + (slider.horizontal
                    ? (slider.availableHeight - height) / 2
                    : slider.visualPosition * (slider.availableHeight - height))
                implicitWidth: Theme.size.sliderHandle
                implicitHeight: Theme.size.sliderHandle
                radius: width / 2
                color: slider.enabled ? Theme.colors.text : Theme.colors.disabledText
            }
        }

        AmplifyButton {
            visible: root.showControls
            variant: AmplifyButton.Variant.Ghost
            text: "+"
            textColor: Theme.colors.textMuted
            padding: 0
            implicitWidth: Theme.size.sliderStepButton
            implicitHeight: Theme.size.sliderStepButton
            radius: Theme.radius.round
            enabled: root.enabled && slider.value < slider.to
            Layout.row: 1
            Layout.column: root.horizontal ? 2 : 0
            Layout.alignment: Qt.AlignCenter
            onClicked: root.adjustBy(1)
        }
    }
}
