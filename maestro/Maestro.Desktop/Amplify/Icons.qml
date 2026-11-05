pragma Singleton
import QtQuick

// SVG icons from https://phosphoricons.com (MIT). Add new ones to icons/ and list them here.
QtObject {
    readonly property url play: Qt.resolvedUrl("icons/play.svg")
    readonly property url pause: Qt.resolvedUrl("icons/pause.svg")
}
