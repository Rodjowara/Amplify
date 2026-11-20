import QtQuick
import QtQuick.Controls.Basic
import QtQuick.Layouts
import Amplify

Item {
    id: root

    required property var errors
    property real bannerBottomMargin: 0

    readonly property int pageContentMaxWidth: 420
    readonly property string message: ErrorMessages.text(errors.code, name => errors.argument(name))

    Rectangle {
        visible: root.errors.showBanner
        color: Theme.colors.error
        radius: Theme.radius.normal
        anchors.left: parent.left
        anchors.right: parent.right
        anchors.bottom: parent.bottom
        anchors.margins: Theme.spacing.normal
        anchors.bottomMargin: Theme.spacing.normal + root.bannerBottomMargin
        implicitHeight: bannerContent.implicitHeight + 2 * Theme.spacing.normal

        RowLayout {
            id: bannerContent
            anchors.fill: parent
            anchors.margins: Theme.spacing.normal
            spacing: Theme.spacing.normal

            Label {
                text: root.message
                color: Theme.colors.text
                font.pixelSize: Theme.fontSize.normal
                wrapMode: Text.WordWrap
                Layout.fillWidth: true
            }

            AmplifyButton {
                variant: AmplifyButton.Variant.Ghost
                display: AbstractButton.IconOnly
                text: qsTr("Close")
                icon.source: Icons.close
                implicitWidth: Theme.size.compactControlHeight
                implicitHeight: Theme.size.compactControlHeight
                radius: Theme.radius.small
                onClicked: root.errors.dismissBanner()
            }
        }
    }

    Rectangle {
        visible: root.errors.showPage
        color: Theme.colors.background
        anchors.fill: parent

        MouseArea {
            anchors.fill: parent
        }

        ColumnLayout {
            anchors.centerIn: parent
            width: Math.min(parent.width - 2 * Theme.spacing.large, root.pageContentMaxWidth)
            spacing: Theme.spacing.large

            Label {
                text: root.message
                color: Theme.colors.text
                font.pixelSize: Theme.fontSize.large
                horizontalAlignment: Text.AlignHCenter
                wrapMode: Text.WordWrap
                Layout.fillWidth: true
            }

            AmplifyButton {
                text: qsTr("Quit")
                backgroundColor: Theme.colors.error
                Layout.fillWidth: true
                onClicked: Qt.quit()
            }
        }
    }
}
