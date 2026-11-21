pragma ComponentBehavior: Bound
import QtQuick
import QtQuick.Layouts
import Amplify

Item {
    id: root

    readonly property int maxContentWidth: 480
    readonly property var sliders: [
        { label: qsTr("Volume / Gain"), from: 0, to: 100, value: 72, stepSize: 1, unit: "%" },
        { label: qsTr("Delay"), from: 0, to: 50, value: 0, stepSize: 1, unit: " ms" },
        { label: qsTr("Crossover"), from: 40, to: 250, value: 120, stepSize: 5, unit: " Hz" }
    ]

    ColumnLayout {
        anchors.centerIn: parent
        width: Math.min(parent.width - 2 * Theme.spacing.large, root.maxContentWidth)
        spacing: Theme.spacing.large

        Repeater {
            model: root.sliders

            AmplifySlider {
                required property var modelData

                label: modelData.label
                from: modelData.from
                to: modelData.to
                value: modelData.value
                stepSize: modelData.stepSize
                unit: modelData.unit
                Layout.fillWidth: true
            }
        }
    }
}
