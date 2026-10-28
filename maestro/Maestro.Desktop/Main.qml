import QtQuick
import QtQuick.Controls
import QtQuick.Layouts

ApplicationWindow {
    id: win
    visible: true
    title: "Qt Bridge for C#"
    width: 365; height: 510
    color: "#121212"

    DummyViewModel {
        id: dummy
    }

    ColumnLayout {
        anchors.fill: parent
        anchors.margins: 24
        spacing: 20

        Label {
            text: "Hello, World!"
            font.pixelSize: 40
            color: "white"
            horizontalAlignment: Text.AlignHCenter
            Layout.alignment: Qt.AlignHCenter
        }

        ColumnLayout {
            Layout.fillWidth: true
            spacing: 6
            Label {
                text: "Welcome to Qt Bridge for C#"
                Layout.fillWidth: true
                horizontalAlignment: Text.AlignHCenter
                font.pixelSize: 24
                color: "#cfcfcf"
            }

            Label {
                text: "QML Multi-language App UI"
                Layout.fillWidth: true
                horizontalAlignment: Text.AlignHCenter
                font.pixelSize: 24
                color: "white"
                wrapMode: Text.WordWrap
            }
        }

        Button {
            id: counterBtn
            text: dummy.clicks === 0 ? "Click me" : "Clicked " + dummy.clicks + " times"

            Layout.fillWidth: true
            Layout.preferredHeight: 48
            font.pixelSize: 16

            hoverEnabled: false
            focusPolicy: Qt.NoFocus

            readonly property color brandGreen: "#41cd52"

            background: Rectangle {
                radius: 12
                border.width: 0
                color: counterBtn.down
                    ? Qt.darker(counterBtn.brandGreen, 1.2)
                    : counterBtn.brandGreen
            }

            contentItem: Text {
                text: counterBtn.text
                color: "white"
                font: counterBtn.font
                horizontalAlignment: Text.AlignHCenter
                verticalAlignment: Text.AlignVCenter
                elide: Text.ElideRight
            }

            onClicked: dummy.clicks += 1
        }

        Button {
            text: "Ping"
            Layout.fillWidth: true
            onClicked: dummy.ping()
        }

        Label {
            text: dummy.pingResponse
            color: "white"
            Layout.fillWidth: true
            horizontalAlignment: Text.AlignHCenter
            wrapMode: Text.WordWrap
        }
    }
}
