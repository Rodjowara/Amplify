import QtQuick

// Small dot that shows whether something (an amp, a connection) is online.
Rectangle {
    property bool online: false

    implicitWidth: Theme.size.statusDot
    implicitHeight: Theme.size.statusDot
    radius: width / 2
    color: online ? Theme.colors.online : Theme.colors.offline
}
