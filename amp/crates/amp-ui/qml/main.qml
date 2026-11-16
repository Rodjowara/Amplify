import QtQuick
import QtQuick.Layouts
import Amplify
import Amp

Window {
    id: window

    readonly property var pages: [
        { title: qsTr("Now Playing"), icon: Icons.music, page: nowPlayingPage },
        { title: qsTr("Channel"), icon: Icons.channel, page: channelPage },
        { title: qsTr("Levels"), icon: Icons.volume, page: levelsPage },
        { title: qsTr("Equalizer"), icon: Icons.equalizer, page: equalizerPage }
    ]

    width: 1480; height: 320; visible: true
    title: "Amp"
    color: Theme.colors.background

    Component { id: nowPlayingPage; NowPlayingPage {} }
    Component { id: channelPage; ChannelPage {} }
    Component { id: levelsPage; LevelsPage {} }
    Component { id: equalizerPage; EqualizerPage {} }

    RowLayout {
        anchors.fill: parent
        spacing: 0

        Sidebar {
            id: sidebar
            pages: window.pages
            Layout.fillHeight: true
        }

        StackLayout {
            currentIndex: sidebar.currentIndex
            Layout.fillWidth: true
            Layout.fillHeight: true

            Repeater {
                model: window.pages

                Loader {
                    required property var modelData
                    sourceComponent: modelData.page
                }
            }
        }
    }
}
