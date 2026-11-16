import QtQuick
import QtQuick.Controls.Basic
import Amplify

// Stand-in for pages that aren't built yet.
Item {
    property string title: ""

    Label {
        anchors.centerIn: parent
        text: parent.title
        color: Theme.colors.textMuted
        font.pixelSize: Theme.fontSize.large
    }
}
