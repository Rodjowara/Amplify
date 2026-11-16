pragma Singleton
import QtQuick

// Look of every Amplify* component. Change a value here and it updates everywhere.
QtObject {
    readonly property QtObject colors: QtObject {
        readonly property color background: "#121212"
        readonly property color surface: "#1e1e1e"
        readonly property color surfaceHover: "#262626"
        readonly property color border: "#333333"
        readonly property color primary: "#41cd52"
        readonly property color primarySubtle: "#1f3322"
        readonly property color primaryText: "#ffffff"
        readonly property color text: "#ffffff"
        readonly property color textMuted: "#cfcfcf"
        readonly property color textSubtle: "#8a8a8a"
        readonly property color disabled: "#3a3a3a"
        readonly property color disabledText: "#8a8a8a"
        readonly property color online: "#41cd52"
        readonly property color offline: "#5a5a5a"
    }

    readonly property QtObject fontSize: QtObject {
        readonly property int small: 12
        readonly property int normal: 16
        readonly property int large: 24
        readonly property int title: 40
    }

    readonly property QtObject radius: QtObject {
        readonly property int small: 6
        readonly property int normal: 12
        readonly property int round: 999
    }

    readonly property QtObject spacing: QtObject {
        readonly property int small: 6
        readonly property int normal: 12
        readonly property int large: 24
    }

    readonly property QtObject size: QtObject {
        readonly property int controlHeight: 48
        readonly property int compactControlHeight: 40
        readonly property int icon: 20
        readonly property int statusDot: 10
        readonly property int sliderTrack: 4
        readonly property int sliderHandle: 14
    }

    // How much darker a control gets while pressed.
    readonly property real pressedDarker: 1.2
}
