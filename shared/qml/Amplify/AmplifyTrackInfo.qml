import QtQuick
import QtQuick.Controls.Basic
import QtQuick.Controls.impl
import QtQuick.Layouts

// Album art with the track title and artist next to it. Used in the Maestro playback dock
// and the amp's Now Playing screen; set artSize and the font sizes to scale it.
RowLayout {
    id: root

    property string title: ""
    property string artist: ""
    property url artSource: ""
    property int artSize: Theme.size.controlHeight
    property int titleSize: Theme.fontSize.normal
    property int artistSize: Theme.fontSize.small

    spacing: Theme.spacing.normal

    Rectangle {
        implicitWidth: root.artSize
        implicitHeight: root.artSize
        radius: Theme.radius.small
        color: Theme.colors.surfaceHover
        clip: true

        Image {
            id: art
            anchors.fill: parent
            source: root.artSource
            fillMode: Image.PreserveAspectCrop
            visible: status === Image.Ready
        }

        // Placeholder when there's no album art.
        IconImage {
            anchors.centerIn: parent
            visible: !art.visible
            source: Icons.music
            color: Theme.colors.textSubtle
            sourceSize.width: root.artSize / 2
            sourceSize.height: root.artSize / 2
        }
    }

    ColumnLayout {
        spacing: 2
        Layout.fillWidth: true

        Label {
            text: root.title !== "" ? root.title : qsTr("Nothing playing")
            color: root.title !== "" ? Theme.colors.text : Theme.colors.textSubtle
            font.pixelSize: root.titleSize
            elide: Text.ElideRight
            Layout.fillWidth: true
        }

        Label {
            visible: root.artist !== ""
            text: root.artist
            color: Theme.colors.textSubtle
            font.pixelSize: root.artistSize
            elide: Text.ElideRight
            Layout.fillWidth: true
        }
    }
}
