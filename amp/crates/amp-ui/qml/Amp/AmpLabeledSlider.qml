import QtQuick
import QtQuick.Layouts
import Amplify

Item {
    id: root

    property alias orientation: slider.orientation
    property alias from: slider.from
    property alias to: slider.to
    property alias value: slider.value
    property alias stepSize: slider.stepSize
    readonly property alias pressed: slider.pressed
    readonly property bool horizontal: slider.horizontal

    property string label: ""
    property string unit: ""
    property string valueText: Math.round(value) + unit

    property alias fillColor: slider.fillColor
    property alias trackColor: slider.trackColor

    readonly property int stepButtonSize: 24
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
        rowSpacing: Theme.spacing.small
        columnSpacing: Theme.spacing.small

        Text {
            visible: root.label !== ""
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
            text: root.valueText
            color: root.enabled ? Theme.colors.textMuted : Theme.colors.disabledText
            font.pixelSize: Theme.fontSize.small
            Layout.row: 0
            Layout.column: root.horizontal ? 2 : 0
            Layout.alignment: root.horizontal ? (Qt.AlignRight | Qt.AlignVCenter) : Qt.AlignHCenter
        }

        AmplifyButton {
            variant: AmplifyButton.Variant.Ghost
            text: "-"
            textColor: Theme.colors.textMuted
            padding: 0
            implicitWidth: root.stepButtonSize
            implicitHeight: root.stepButtonSize
            radius: Theme.radius.round
            enabled: root.enabled && slider.value > slider.from
            Layout.row: root.horizontal ? 1 : 3
            Layout.column: 0
            Layout.alignment: Qt.AlignCenter
            onClicked: root.adjustBy(-1)
        }

        AmplifySlider {
            id: slider
            Layout.row: root.horizontal ? 1 : 2
            Layout.column: root.horizontal ? 1 : 0
            Layout.fillWidth: root.horizontal
            Layout.fillHeight: !root.horizontal
            Layout.alignment: Qt.AlignCenter
            onMoved: root.moved()
        }

        AmplifyButton {
            variant: AmplifyButton.Variant.Ghost
            text: "+"
            textColor: Theme.colors.textMuted
            padding: 0
            implicitWidth: root.stepButtonSize
            implicitHeight: root.stepButtonSize
            radius: Theme.radius.round
            enabled: root.enabled && slider.value < slider.to
            Layout.row: 1
            Layout.column: root.horizontal ? 2 : 0
            Layout.alignment: Qt.AlignCenter
            onClicked: root.adjustBy(1)
        }
    }
}
