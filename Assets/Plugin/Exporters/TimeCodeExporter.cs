using System.Collections.Generic;
using UnityEngine;
using RtMidi;
using HNode.Util.Midi;
using System;
using System.Net.Sockets;
using System.Net;
using TMPro;

public class TimeCodeExporter : IExporter
{
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    public string midiDevice = "loopMIDI Port"; //Default to no device selected
#elif UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
    public string midiDevice = "Virtual RawMIDI"; //Default to Reaper on Linux
#endif

    private MidiIn midiInput;
    private List<UdpClient> udpClients = new List<UdpClient>();
    private int[] ports = new int[] { 7001, 7002, 7003, 7004, 7005 };

    public void MidiConnectDevice(string device)
    {
        //This is really only useful if you're changing devices
        //Reconnecting to the same device you're already connected to throws an exception on windows
        if (midiInput != null)
        {
            UnityEngine.Debug.Log("Dispose");
            midiInput.ClosePort();
            midiInput.Dispose();
            midiInput = null;
            //force GC call
            GC.Collect();
        }

        //commented out to let exceptions through
        try
        {
            midiInput = MidiIn.Create();
            midiInput.OpenPort(MidiUtil.GetMidiPort(midiInput, device), "HNode");
            midiInput.IgnoreTypes(false, false, false);

            midiInput.MessageReceived = (someRandomDoubleIdontUnderstand, midiPacket) => {
                MidiEvent midiEvent = MidiUtil.DeserializeMidi(midiPacket);

                switch (midiEvent.midiCode)
                {
                    case MidiCode.TimeCodeQuarterFrame:
                        OnTimeQuarterFrame(midiEvent);
                        break;

                    case MidiCode.SystemExclusive:
                        OnFullFrame(midiPacket);
                        break;
                    
                    default:
                        break;
                }
            };
        }
        catch (Exception ex)
        {
            Debug.LogError("Error connecting to MIDI device: " + ex.Message);
        }
    }

    //this isnt good, theres gotta be a way to unstatic this
    //maybe concurrentdictionary with key as port I guess?
    private static TimeSpan timeCode = TimeSpan.Zero;
    private static byte frames = 0;
    private static Dictionary<MidiTimeCodePiece, int> timeSegments = new();

    // Inspired from: https://github.com/melanchall/drywetmidi/blob/46b2ac61b2f9c0efb9a6a43f0542d7a64bf7ef73/DryWetMidi/Multimedia/InputEndpoint/InputEndpoint.cs#L714
    private void OnTimeQuarterFrame(MidiEvent midiEvent)
    {
        MidiTimeCodePiece timeCodePiece = (MidiTimeCodePiece)(midiEvent.noteNumber >> 4);

        timeSegments[timeCodePiece] = midiEvent.noteNumber & 0xF;

        if (timeSegments.Count == 8)
        {
            int midiFrames = (timeSegments[MidiTimeCodePiece.FrameMostSignificant] << 4) | timeSegments[MidiTimeCodePiece.FrameLeastSignificant];
            int midiSeconds = (timeSegments[MidiTimeCodePiece.SecondMostSignificant] << 4) | timeSegments[MidiTimeCodePiece.SecondLeastSignificant];
            int midiMinutes = (timeSegments[MidiTimeCodePiece.MinuteMostSignificant] << 4) | timeSegments[MidiTimeCodePiece.MinuteLeastSignificant];
            int midiHours = (timeSegments[MidiTimeCodePiece.RateHourMostSignificant] << 4) | timeSegments[MidiTimeCodePiece.HourLeastSignificant];

            float framerate = (midiHours & 0x60) switch
            {
                0x00 => 24f,
                0x20 => 25f,
                0x40 => 29.97f,
                0x60 => 30f,
                _ => 30f, //assume 30 I guess, this should never happen
            };

            //convert the timecode to a TimeSpan
            timeCode = new TimeSpan(0, midiHours & 0x1F, midiMinutes, midiSeconds);
            frames = (byte)midiFrames;

            timeSegments.Clear();
        }
    }

    private void OnFullFrame(ReadOnlySpan<byte> midiPacket)
    {
        //length should be 10
        if (midiPacket.Length != 10) return;

        //first two bytes should be 7F 7F
        if (midiPacket[1] != 0x7F) return;
        if (midiPacket[2] != 0x7F) return;
        //third & fourth bytes should be 01 01
        if (midiPacket[3] != 0x01) return;
        if (midiPacket[4] != 0x01) return;

        //top 3 bits of hours contain framerate info
        //split out the hours byte
        int midiHours = midiPacket[5] & 0x1F; // Mask out the top 3 bits
        int midiMinutes = midiPacket[6] & 0x7F;
        int midiSeconds = midiPacket[7] & 0x7F;
        int midiFrames = midiPacket[8] & 0x7F;

        float framerate = (midiHours & 0x60) switch
        {
            0x00 => 24f,
            0x20 => 25f,
            0x40 => 29.97f,
            0x60 => 30f,
            _ => 30f,//assume 30 I guess, this should never happen
        };

        //convert the timecode to a TimeSpan
        timeCode = new TimeSpan(0, midiHours, midiMinutes, midiSeconds);
        frames = (byte)midiFrames;
    }

    public void CompleteFrame(ref List<byte> channelValues)
    {
        // throw new System.NotImplementedException();
    }

    public void Construct()
    {
        UnityEngine.Debug.Log("Connect: " + midiDevice);
        MidiConnectDevice(midiDevice);

        foreach (var port in ports)
        {
            UnityEngine.Debug.Log("Connecting to UDP port: " + port);

            var udpClient = new UdpClient();

            try {
                udpClient.Connect(IPAddress.Loopback, port);
            } catch {}

            udpClients.Add(udpClient);
        }
    }

    private protected TMP_InputField midiDeviceField;
    public void ConstructUserInterface(RectTransform rect)
    {
        midiDeviceField = Util.AddInputField(rect, "MIDI Device")
            .WithText(midiDevice)
            .WithCallback((value) => { midiDevice = value; });
        
        var reconnectButton = Util.AddButton(rect, "Reconnect MIDI Device");
        reconnectButton.onClick.AddListener(() =>
        {
            UnityEngine.Debug.Log("Reconnecting MIDI Device...");
            MidiConnectDevice(midiDevice);
        });
    }

    public void Deconstruct()
    {
        midiInput?.ClosePort();
        midiInput?.Dispose();

        foreach (var udpClient in udpClients)
        {
            udpClient?.Close();
        }

        udpClients.Clear();
    }

    public void DeconstructUserInterface()
    {
        // throw new System.NotImplementedException();
    }

    public static byte[] IntToBigEndianBytes(int value)
    {
        // Get the bytes based on the host system's architecture.
        byte[] bytes = BitConverter.GetBytes(value);
        
        // C# runs mostly on little-endian systems. Network byte order is big-endian.
        // If the host is little-endian, we need to reverse the bytes to get big-endian order.
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        return bytes;
    }

    public static byte[] LongToBigEndianBytes(ulong value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        return bytes;
    }

    public void InitFrame(ref List<byte> channelValues)
    {
        //not a huge difference from here compared to doing it in complete frame, but this means there should in theory be ever so slightly less latency if whoever is receiving this is writing DMX
        //send a UDP packet with the timecode data as purely a UTC time since epoch
        int utcMillis = (int)(timeCode.TotalMilliseconds);

        //get the current UTC time aswell
        ulong currentUtcMillis = (ulong)(DateTime.UtcNow - DateTime.UnixEpoch).TotalMilliseconds;

        List<byte> data = new List<byte>();
        data.AddRange(IntToBigEndianBytes(utcMillis));
        //add frames as the 5th byte
        data.Add(frames);
        data.AddRange(LongToBigEndianBytes(currentUtcMillis));

        // Debug.Log(timeCode);

        //try to send
        foreach (var udpClient in udpClients)
        {
            try {
                udpClient.Send(data.ToArray(), data.Count);
            } catch {}
        }
    }

    public void FrameRendered(ref Texture2D texture) {}

    public void SerializeChannel(byte channelValue, int channel)
    {
        // throw new System.NotImplementedException();
    }

    public void UpdateUserInterface()
    {
        // throw new System.NotImplementedException();
    }
}
