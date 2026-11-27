import QtQuick
import QtQuick.Controls.Basic
import QtQuick.Layouts
import Amplify
import components
import pages

ApplicationWindow {
    id: win
    visible: true
    title: "Maestro"
    width: 1100; height: 700
    minimumWidth: 640; minimumHeight: 480
    color: Theme.colors.background

    // Colors for plain Qt controls (TextField, ToolTip...) that don't use the Theme directly.
    palette {
        window: Theme.colors.background
        windowText: Theme.colors.text
        base: Theme.colors.surfaceHover
        text: Theme.colors.text
        button: Theme.colors.surfaceHover
        buttonText: Theme.colors.text
        highlight: Theme.colors.primary
        placeholderText: Theme.colors.textSubtle
        toolTipBase: Theme.colors.surfaceHover
        toolTipText: Theme.colors.text
    }

    QtSynchronizationContext {
        function executeLate() { execute() }
        onPendingChanged: Qt.callLater(executeLate)
    }

    DummyViewModel {
        id: dummy
    }

    PlaybackViewModel {
        id: playback
    }

    SidebarViewModel {
        id: nav
    }

    ErrorViewModel {
        id: errorModel
    }

    SpotifyViewModel {
        id: spotify
    }

    ColumnLayout {
        anchors.fill: parent
        spacing: 0

        RowLayout {
            spacing: 0
            Layout.fillWidth: true
            Layout.fillHeight: true

            Sidebar {
                vm: nav
                Layout.fillHeight: true
                onAddAmpRequested: addAmpDialog.open()
            }

            // One page per nav.currentPage
            StackLayout {
                currentIndex: ["playlist", "settings", "tuning", "debug"].indexOf(nav.currentPage)
                Layout.fillWidth: true
                Layout.fillHeight: true

                PlaylistPage {
                    dummy: dummy
                    playback: playback
                }
                PlaceholderPage { title: qsTr("Settings") }
                PlaceholderPage { title: qsTr("Tuning") + ": " + nav.selectedAmpIp }
                DebugPage { spotify: spotify }
            }
        }

        // TODO: previous/next, shuffle/repeat, seek and volume need backend support (playlist, master volume).
        PlaybackDock {
            id: dock
            title: playback.trackTitle
            playing: playback.playing
            Layout.fillWidth: true
            onPlayPauseClicked: playback.togglePlaying()
        }
    }

    AddAmpDialog {
        id: addAmpDialog
        vm: nav
    }

    ErrorView {
        errors: errorModel
        bannerBottomMargin: dock.height
        anchors.fill: parent
    }
}
