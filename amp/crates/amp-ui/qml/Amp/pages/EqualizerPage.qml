pragma ComponentBehavior: Bound
import QtQuick
import QtQuick.Layouts
import Amplify

Item {
    id: root

    readonly property var bands: ["31", "62", "125", "250", "500", "1k", "2k", "4k", "8k", "16k"]
    readonly property real minGainDb: -12
    readonly property real maxGainDb: 12
    readonly property real gainStepDb: 1

    RowLayout {
        anchors.fill: parent
        anchors.margins: Theme.spacing.large
        spacing: Theme.spacing.normal

        Repeater {
            model: root.bands

            AmplifySlider {
                required property string modelData

                orientation: Qt.Vertical
                label: modelData
                from: root.minGainDb
                to: root.maxGainDb
                value: 0
                stepSize: root.gainStepDb
                Layout.fillWidth: true
                Layout.fillHeight: true
            }
        }
    }
}
