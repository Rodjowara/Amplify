import QtQuick
import QtQuick.Controls.Basic
import QtQuick.Layouts
import Amplify

Item {
    required property var spotify

    readonly property int searchDebounceMs: 300

    Timer {
        id: searchDebounce
        interval: searchDebounceMs
        onTriggered: if (searchField.text.trim() !== "") spotify.search(searchField.text)
    }

    ColumnLayout {
        anchors.fill: parent
        anchors.margins: Theme.spacing.normal
        spacing: Theme.spacing.normal

        AmplifyButton {
            text: qsTr("Connect Spotify")
            onClicked: spotify.connect()
        }

        TextField {
            id: searchField
            placeholderText: qsTr("Search tracks")
            Layout.fillWidth: true
            onTextChanged: searchDebounce.restart()
        }

        ScrollView {
            Layout.fillWidth: true
            Layout.fillHeight: true

            TextArea {
                text: spotify.result
                readOnly: true
                selectByMouse: true
                wrapMode: TextEdit.Wrap
            }
        }
    }
}
