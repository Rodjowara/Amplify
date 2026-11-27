import QtQuick
import Amplify

Item {
    Column {
        anchors.centerIn: parent
        spacing: Theme.spacing.large

        Row {
            spacing: Theme.spacing.small

            AmplifyButton {
                text: qsTr("Left")
                checkable: true
                checked: true
            }

            AmplifyButton {
                text: qsTr("Right")
                checkable: true
            }

            AmplifyButton {
                text: qsTr("Woofer")
                checkable: true
            }
        }

        Row {
            spacing: Theme.spacing.large

            AmplifyCheckBox {
                text: qsTr("Mute")
            }

            AmplifyCheckBox {
                text: qsTr("Solo")
            }
        }
    }
}
