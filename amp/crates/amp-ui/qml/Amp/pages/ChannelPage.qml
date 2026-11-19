import QtQuick
import Amplify

Item {
    Row {
        anchors.centerIn: parent
        spacing: Theme.spacing.large

        AmplifyCheckBox {
            text: qsTr("Mute")
        }

        AmplifyCheckBox {
            text: qsTr("Solo")
        }
    }
}
