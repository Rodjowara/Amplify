import QtQuick
import QtQuick.Controls.Basic
import QtQuick.Layouts
import Amplify

// Bottom dock, visible on every page: track info, transport controls, seek bar and volume.
// It only shows state and emits signals; the parent connects them to the view model.
Rectangle {
    id: root

    property string title: ""
    property string artist: ""
    property url artSource: ""
    property bool playing: false
    property bool shuffle: false
    property bool repeat: false
    property real position: 0     // seconds
    property real duration: 0     // seconds
    property real volume: 1       // 0.0 - 1.0

    signal playPauseClicked()
    signal previousClicked()
    signal nextClicked()
    signal shuffleToggled(bool on)
    signal repeatToggled(bool on)
    signal seekRequested(real seconds)
    signal volumeChangeRequested(real value)

    // Below this width the volume slider turns into a button with a popup.
    readonly property bool compact: width < 760

    implicitHeight: 96
    color: Theme.colors.surface

    function formatTime(seconds) {
        const s = Math.max(0, Math.floor(seconds))
        return Math.floor(s / 60) + ":" + String(s % 60).padStart(2, "0")
    }

    // Top border
    Rectangle {
        width: parent.width
        height: 1
        color: Theme.colors.border
    }

    ColumnLayout {
        anchors.fill: parent
        anchors.leftMargin: Theme.spacing.large
        anchors.rightMargin: Theme.spacing.large
        anchors.topMargin: Theme.spacing.small
        anchors.bottomMargin: Theme.spacing.small
        spacing: 0

        // Seek bar across the full width
        RowLayout {
            spacing: Theme.spacing.normal
            Layout.fillWidth: true

            Label {
                text: root.formatTime(seek.pressed ? seek.value : root.position)
                color: Theme.colors.textSubtle
                font.pixelSize: Theme.fontSize.small
            }

            AmplifySlider {
                id: seek
                from: 0
                to: Math.max(root.duration, 1)
                value: root.position
                enabled: root.duration > 0
                Layout.fillWidth: true
                onMoved: root.seekRequested(value)
            }

            Label {
                text: root.formatTime(root.duration)
                color: Theme.colors.textSubtle
                font.pixelSize: Theme.fontSize.small
            }
        }

        RowLayout {
            spacing: Theme.spacing.normal
            Layout.fillWidth: true
            Layout.fillHeight: true

            AmplifyTrackInfo {
                title: root.title
                artist: root.artist
                artSource: root.artSource
                Layout.preferredWidth: 1
                Layout.fillWidth: true
            }

            // Transport controls, centered
            RowLayout {
                spacing: Theme.spacing.small
                Layout.alignment: Qt.AlignHCenter

                AmplifyButton {
                    variant: AmplifyButton.Variant.Ghost
                    display: AbstractButton.IconOnly
                    checkable: true
                    checked: root.shuffle
                    text: qsTr("Shuffle")
                    icon.source: Icons.shuffle
                    implicitWidth: Theme.size.compactControlHeight
                    implicitHeight: Theme.size.compactControlHeight
                    radius: Theme.radius.round
                    onToggled: root.shuffleToggled(checked)
                }

                AmplifyButton {
                    variant: AmplifyButton.Variant.Ghost
                    display: AbstractButton.IconOnly
                    text: qsTr("Previous")
                    icon.source: Icons.previous
                    implicitWidth: Theme.size.compactControlHeight
                    implicitHeight: Theme.size.compactControlHeight
                    radius: Theme.radius.round
                    onClicked: root.previousClicked()
                }

                AmplifyPlayPauseButton {
                    playing: root.playing
                    toggleOnClick: false
                    onClicked: root.playPauseClicked()
                }

                AmplifyButton {
                    variant: AmplifyButton.Variant.Ghost
                    display: AbstractButton.IconOnly
                    text: qsTr("Next")
                    icon.source: Icons.next
                    implicitWidth: Theme.size.compactControlHeight
                    implicitHeight: Theme.size.compactControlHeight
                    radius: Theme.radius.round
                    onClicked: root.nextClicked()
                }

                AmplifyButton {
                    variant: AmplifyButton.Variant.Ghost
                    display: AbstractButton.IconOnly
                    checkable: true
                    checked: root.repeat
                    text: qsTr("Repeat")
                    icon.source: Icons.repeat
                    implicitWidth: Theme.size.compactControlHeight
                    implicitHeight: Theme.size.compactControlHeight
                    radius: Theme.radius.round
                    onToggled: root.repeatToggled(checked)
                }
            }

            // Volume: slider on wide windows, button with a popup slider when narrow
            RowLayout {
                spacing: Theme.spacing.small
                Layout.preferredWidth: 1
                Layout.fillWidth: true

                Item { Layout.fillWidth: true }

                AmplifyButton {
                    id: volumeButton
                    variant: AmplifyButton.Variant.Ghost
                    display: AbstractButton.IconOnly
                    text: qsTr("Volume")
                    icon.source: Icons.volume
                    implicitWidth: Theme.size.compactControlHeight
                    implicitHeight: Theme.size.compactControlHeight
                    radius: Theme.radius.round
                    enabled: root.compact
                    icon.color: Theme.colors.text
                    onClicked: volumePopup.open()

                    Popup {
                        id: volumePopup
                        y: -height - Theme.spacing.small
                        x: (parent.width - width) / 2
                        padding: Theme.spacing.normal
                        background: Rectangle {
                            color: Theme.colors.surface
                            radius: Theme.radius.normal
                            border.color: Theme.colors.border
                        }

                        AmplifySlider {
                            orientation: Qt.Vertical
                            implicitHeight: 140
                            value: root.volume
                            onMoved: root.volumeChangeRequested(value)
                        }
                    }
                }

                AmplifySlider {
                    visible: !root.compact
                    value: root.volume
                    Layout.preferredWidth: 120
                    onMoved: root.volumeChangeRequested(value)
                }
            }
        }
    }
}
