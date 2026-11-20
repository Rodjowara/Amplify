import QtQuick
import QtQuick.Controls.Basic
import QtQuick.Layouts
import Amplify

Dialog {
    id: root

    property string acceptText: qsTr("OK")
    property bool acceptEnabled: true
    property string errorText: ""
    property real preferredWidth: 420

    signal acceptClicked()

    parent: Overlay.overlay
    anchors.centerIn: parent
    width: Math.min(parent.width - 2 * Theme.spacing.large, preferredWidth)
    modal: true
    closePolicy: Popup.CloseOnEscape
    padding: Theme.spacing.large

    background: Rectangle {
        color: Theme.colors.surface
        radius: Theme.radius.normal
        border.color: Theme.colors.border
    }

    header: Control {
        padding: Theme.spacing.large
        bottomPadding: 0

        contentItem: RowLayout {
            spacing: Theme.spacing.normal

            Label {
                text: root.title
                color: Theme.colors.text
                font.pixelSize: Theme.fontSize.large
                font.bold: true
                elide: Text.ElideRight
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
                onClicked: root.reject()
            }
        }
    }

    footer: Control {
        padding: Theme.spacing.large
        topPadding: 0

        contentItem: ColumnLayout {
            spacing: Theme.spacing.normal

            Control {
                visible: root.errorText !== ""
                padding: Theme.spacing.normal
                Layout.fillWidth: true

                background: Rectangle {
                    color: Theme.colors.error
                    radius: Theme.radius.small
                }

                contentItem: Label {
                    text: root.errorText
                    color: Theme.colors.text
                    font.pixelSize: Theme.fontSize.normal
                    wrapMode: Text.WordWrap
                }
            }

            RowLayout {
                spacing: Theme.spacing.normal
                Layout.fillWidth: true

                Item {
                    Layout.fillWidth: true
                }

                AmplifyButton {
                    variant: AmplifyButton.Variant.Ghost
                    text: qsTr("Cancel")
                    horizontalPadding: Theme.spacing.large
                    onClicked: root.reject()
                }

                AmplifyButton {
                    text: root.acceptText
                    enabled: root.acceptEnabled
                    horizontalPadding: Theme.spacing.large
                    onClicked: root.acceptClicked()
                }
            }
        }
    }
}
