using System;
using System.Collections.Generic;
using RtMidi;

namespace HNode.Util.Midi
{
    public enum MidiCode : int
    {
        // https://midi.org/summary-of-midi-1-0-messages
        // Channel Voice Messages [nnnn = 0-15 (MIDI Channel Number 1-16)] 
        NoteOff = 128,
        NoteOn = 144,
        PolyphonicKeyPressure = 160,
        ControlChange = 176,
        ProgramChange = 192,
        ChannelPressureAfterTouch = 208,
        PitchBendChange = 224,

        // Channel Mode Messages (See also Control Change, above)
        // ChannelModeMessages = 176,

        // System Common Messages
        SystemExclusive = 240,
        TimeCodeQuarterFrame = 241,
        SongPositionPointer = 242,
        SongSelect = 243,
        // Undefined = 244,
        // Undefined = 245,
        TuneRequest = 246,
        EndOfExclusive = 247,


        // System Real-Time Messages
        TimingClock = 248,
        // Undefined = 249,
        Start = 250,
        Continue = 251,
        Stop = 252,
        // Undefined = 253,
        ActiveSensing = 254,
        Reset = 255
    }

    public enum MidiTimeCodePiece : int
    {
        FrameLeastSignificant = 0x0,
        FrameMostSignificant = 0x1,

        SecondLeastSignificant = 0x2,
        SecondMostSignificant = 0x3,
        
        MinuteLeastSignificant = 0x4,
        MinuteMostSignificant = 0x5,

        HourLeastSignificant = 0x6,
        RateHourMostSignificant = 0x7
    }

    public static class MidiUtil
    {
        /// <summary>
        /// Serialize a Midi packet and send it (without checks)
        /// </summary>
        /// <param name="midiOutput"></param>
        /// <param name="midiCode">Midi Message Code https://midi.org/summary-of-midi-1-0-messages</param>
        /// <param name="channel">(0-15)</param>
        /// <param name="noteNumber">(0-127)</param>
        /// <param name="value">(0-127)</param>
        public static void SendMidiUnsafe(MidiOut midiOutput, MidiCode midiCode, int channel, int noteNumber, int value)
        {
            midiOutput.SendMessage(SerializeMidi(midiCode, channel, noteNumber, value));
        }

        /// <summary>
        /// Serialize a Midi packet and send it (with checks)
        /// </summary>
        /// <param name="midiOutput"></param>
        /// <param name="midiCode">Midi Message Code https://midi.org/summary-of-midi-1-0-messages</param>
        /// <param name="channel">(0-15)</param>
        /// <param name="noteNumber">(0-127)</param>
        /// <param name="value">(0-127)</param>
        public static void SendMidi(MidiOut midiOutput, MidiCode midiCode, int channel, int noteNumber, int value)
        {
            if (midiOutput == null)
                return;
            
            if (midiOutput.IsInvalid || midiOutput.IsClosed)
                return;
            
            if (midiOutput.IsOk)
                SendMidiUnsafe(midiOutput, midiCode, channel, noteNumber, value);
        }

        /// <summary>
        /// Serialize a Midi packet and send it (with checks)
        /// </summary>
        /// <param name="midiOutput"></param>
        /// <param name="midiEvent"></param>
        public static void SendMidi(MidiOut midiOutput, MidiEvent midiEvent)
        {
            SendMidi(midiOutput, midiEvent.midiCode, midiEvent.channel, midiEvent.noteNumber, midiEvent.value);
        }

        /// <summary>
        /// Serialize a Midi message packet into 3 bytes
        /// </summary>
        /// <param name="midiCode">Midi Message Code https://midi.org/summary-of-midi-1-0-messages</param>
        /// <param name="channel">(0-15)</param>
        /// <param name="noteNumber">(0-127)</param>
        /// <param name="value">(0-127)</param>
        /// <returns>3 bytes</returns>
        public static byte[] SerializeMidi(MidiCode midiCode, int channel, int noteNumber, int value)
        {
            byte[] serializedMidi = new byte[3];

            // RTFM, https://midi.org/summary-of-midi-1-0-messages
            serializedMidi[0] = (byte)(((int)midiCode & 0xF0) | (channel & 0xF)); // 4 bits midi code followed by 4 bits channel
            serializedMidi[1] = (byte)(noteNumber & 0x7F); // NoteNumber
            serializedMidi[2] = (byte)(value & 0x7F); // Value

            return serializedMidi;
        }

        /// <summary>
        /// Deserialize 1-3 bytes into a Midi message packet.
        /// </summary>
        /// <param name="midiPacket">1-3 bytes</param>
        /// <returns></returns>
        public static MidiEvent DeserializeMidi(ReadOnlySpan<byte> midiPacket)
        {
            int midiControlByte = midiPacket[0] & 0xF0;
            bool systemMessage = midiControlByte == 0xF0;

            MidiCode midiCode = (MidiCode)(systemMessage ? midiPacket[0] : midiControlByte);

            int channel = systemMessage ? 0 : midiPacket[0];
            int noteNumber = midiPacket.Length > 1 ? midiPacket[1] : 0;
            int value = midiPacket.Length > 2 ? midiPacket[2] : 0;

            return new MidiEvent(midiCode, channel, noteNumber, value);
        }

        /// <summary>
        /// Gets a list of available Midi ports
        /// </summary>
        /// <param name="midiPort"></param>
        /// <returns>Available Midi ports of the opposite type</returns>
        public static List<string> GetMidiDevices(MidiBase midiPort)
        {
            List<string> midiDevices = new List<string> { "(none)" };

            for (int indx = 0; indx < midiPort.PortCount; indx++)
                midiDevices.Add(midiPort.GetPortName(indx));

            return midiDevices;
        }

        /// <summary>
        /// Gets the port number for a device name
        /// </summary>
        /// <param name="midiPort"></param>
        /// <param name="deviceName">Full or partial name of the device</param>
        /// <returns>Port number of the device, or -1 if none</returns>
        public static int GetMidiPort(MidiBase midiPort, string deviceName)
        {
            List<string> midiDevices = GetMidiDevices(midiPort);

            for (int indx = 0; indx < midiDevices.Count; indx++)
                if (midiDevices[indx].Contains(deviceName))
                    return indx - 1;
            
            return -1;
        }
    }

    public class MidiEvent
    {
        public readonly MidiCode midiCode;
        public readonly int channel;
        public readonly int noteNumber;
        public readonly int value;

        /// <summary>
        /// Create a MidiEvent
        /// </summary>
        /// <param name="_midiCode"></param>
        /// <param name="_channel">(0-15)</param>
        /// <param name="_noteNumber">(0-127)</param>
        /// <param name="_value">(0-127)</param>
        public MidiEvent(MidiCode _midiCode = MidiCode.Reset, int _channel = 0, int _noteNumber = 0, int _value = 0)
        {
            midiCode = _midiCode;
            channel = _channel & 0xF;
            noteNumber = _noteNumber & 0x7F;
            value = _value & 0x7F;
        }
    }
}