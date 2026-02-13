using System.Collections.Generic;
using UnityEngine;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Composing;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Standards;
using System;
using System.Net.Sockets;
using System.Net;
using TMPro;

public class TimeCodeExporter : IExporter
{
    public string midiDevice = "loopMIDI Port"; //Default to no device selected

    private InputDevice midiInput;
    private UdpClient udpClient;
    private int port = 7001;

    public void MidiConnectDevice(string device)
    {
        //This is really only useful if you're changing devices
        //Reconnecting to the same device you're already connected to throws an exception on windows
        if (midiInput != null)
        {
            UnityEngine.Debug.Log("Dispose");
            midiInput.Dispose();
            midiInput = null;
            //force GC call
            GC.Collect();
        }

        //commented out to let exceptions through
        try
        {
            midiInput = InputDevice.GetByName(device);
            midiInput.RaiseMidiTimeCodeReceived = true;
            midiInput.MidiTimeCodeReceived += OnEventReceived;
            midiInput.EventReceived += OnFullFrame;
            midiInput.StartEventsListening();
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

    private static void OnEventReceived(object sender, MidiTimeCodeReceivedEventArgs e)
    {
        //try to convert the sender to ourself
        var exporter = sender as TimeCodeExporter;

        var framerate = e.Format switch
        {
            MidiTimeCodeType.Thirty => 30f,
            MidiTimeCodeType.ThirtyDrop => 29.97f,
            MidiTimeCodeType.TwentyFive => 25f,
            MidiTimeCodeType.TwentyFour => 24f,
            _ => 30f,//assume 30 I guess, this should never happen
        };
        //convert the timecode to a TimeSpan
        timeCode = new TimeSpan(0, e.Hours, e.Minutes, e.Seconds);
        frames = (byte)e.Frames;
    }

    private static void OnFullFrame(object? sender, MidiEventReceivedEventArgs e)
    {
        //check type
        if (e.Event.EventType != MidiEventType.NormalSysEx)
        {
            return;
        }

        var sysExEvent = e.Event as SysExEvent;
        var data = sysExEvent.Data;

        //convert the data to hex string
        string hexString = BitConverter.ToString(data);
        // Debug.Log(hexString);

        //length should be 9
        if (data.Length != 9)
            return;

        //first two bytes should be 7F 7F
        if (data[0] != 0x7F) return;
        if (data[1] != 0x7F) return;
        //third byte should be 01
        if (data[2] != 0x01) return;
        //fourth byte should be 01
        if (data[3] != 0x01) return;

        //top 3 bits of hours contain framerate info
        //split out the hours byte
        var hours = data[4] & 0x1F; // Mask out the top 3 bits
        var minutes = data[5];
        var seconds = data[6];
        var framesData = data[7];

        // Debug.Log($"Full Frame Timecode Received: {hours}:{minutes}:{seconds}:{frames}");

        float framerate = (hours & 0x60) switch
        {
            0x00 => 24f,
            0x20 => 25f,
            0x40 => 30f,
            0x60 => 29.97f,
            _ => 30f,//assume 30 I guess, this should never happen
        };
        //convert the timecode to a TimeSpan
        timeCode = new TimeSpan(0, hours, minutes, seconds);
        frames = (byte)framesData;
    }

    public void CompleteFrame(ref List<byte> channelValues)
    {
        // throw new System.NotImplementedException();
    }

    public void Construct()
    {
        UnityEngine.Debug.Log("Connect: " + midiDevice);
        MidiConnectDevice(midiDevice);

        udpClient = new UdpClient();
        udpClient.Connect(IPAddress.Loopback, port);
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
        midiInput?.Dispose();
        udpClient?.Close();
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

    public void InitFrame(ref List<byte> channelValues)
    {
        //not a huge difference from here compared to doing it in complete frame, but this means there should in theory be ever so slightly less latency if whoever is receiving this is writing DMX
        //send a UDP packet with the timecode data as purely a UTC time since epoch
        int utcMillis = (int)(timeCode.TotalMilliseconds);

        List<byte> data = new List<byte>();
        data.AddRange(IntToBigEndianBytes(utcMillis));
        //add frames as the 5th byte
        data.Add(frames);

        // Debug.Log(timeCode);

        //try to send
        udpClient.Send(data.ToArray(), data.Count);
    }

    public void SerializeChannel(byte channelValue, int channel)
    {
        // throw new System.NotImplementedException();
    }

    public void UpdateUserInterface()
    {
        // throw new System.NotImplementedException();
    }
}
