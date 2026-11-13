pragma Singleton
import QtQuick

// SVG icons from https://phosphoricons.com (MIT). Add new ones to icons/ and list them here.
QtObject {
    readonly property url play: Qt.resolvedUrl("icons/play.svg")
    readonly property url pause: Qt.resolvedUrl("icons/pause.svg")
    readonly property url previous: Qt.resolvedUrl("icons/previous.svg")
    readonly property url next: Qt.resolvedUrl("icons/next.svg")
    readonly property url shuffle: Qt.resolvedUrl("icons/shuffle.svg")
    readonly property url repeat: Qt.resolvedUrl("icons/repeat.svg")
    readonly property url volume: Qt.resolvedUrl("icons/volume.svg")
    readonly property url playlist: Qt.resolvedUrl("icons/playlist.svg")
    readonly property url settings: Qt.resolvedUrl("icons/settings.svg")
    readonly property url plus: Qt.resolvedUrl("icons/plus.svg")
    readonly property url music: Qt.resolvedUrl("icons/music.svg")
}
