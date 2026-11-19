pragma Singleton
import QtQuick

QtObject {
    function text(code, argument) {
        switch (code) {
        case "InvalidAmpAddress": return qsTr("\"%1\" is not a valid amp address.").arg(argument("address"))
        case "AmpUnreachable": return qsTr("Can't reach amp %1. Check that it is on and in the same network.").arg(argument("amp"))
        case "AmpTimeout": return qsTr("Amp %1 did not answer in time.").arg(argument("amp"))
        case "AmpRejectedRequest": return qsTr("Amp %1 rejected the request.").arg(argument("amp"))
        case "FileNotFound": return qsTr("The file \"%1\" could not be found.").arg(argument("path"))
        case "FfmpegNotFound": return qsTr("ffmpeg is not installed. Install it and restart Maestro.")
        case "SnapserverUnreachable": return qsTr("Can't connect to the audio server.")
        case "StreamInterrupted": return qsTr("Playback stopped because the connection to the audio server was lost.")
        case "SnapserverStartFailed": return qsTr("Couldn't start the audio server. Make sure Docker is installed and running.")
        default: return qsTr("Something went wrong.")
        }
    }
}
