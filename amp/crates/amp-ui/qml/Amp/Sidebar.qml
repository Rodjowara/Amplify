import QtQuick
import QtQuick.Layouts
import Amplify

Rectangle {
    id: root

    required property var pages
    property int currentIndex: 0

    readonly property int buttonSize: 64
    readonly property int iconSize: 32

    color: Theme.colors.surface
    implicitWidth: sidebarColumn.implicitWidth + 2 * Theme.spacing.normal

    ColumnLayout {
        id: sidebarColumn
        anchors.fill: parent
        anchors.margins: Theme.spacing.normal
        spacing: Theme.spacing.small

        Repeater {
            model: root.pages

            Item {
                required property var modelData
                required property int index

                implicitWidth: navButton.implicitWidth
                implicitHeight: navButton.implicitHeight
                Layout.fillHeight: index > 0

                AmplifyNavButton {
                    id: navButton
                    anchors.bottom: parent.bottom
                    anchors.horizontalCenter: parent.horizontalCenter
                    iconOnly: true
                    implicitHeight: root.buttonSize
                    icon.width: root.iconSize
                    icon.height: root.iconSize
                    text: parent.modelData.title
                    icon.source: parent.modelData.icon
                    active: parent.index === root.currentIndex
                    onClicked: root.currentIndex = parent.index
                }
            }
        }
    }
}
