using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Composing;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Standards;
using UnityEngine;

// Requires MIDIDMX installed in a world
// https://github.com/micksam7/VRC-MIDIDMX
// Original code by Micca
// me@micksam7.com

//This code was ported from a non-unity project so it may be very slightly weird :)

public class MIDIDMX : IExporter
{
    public bool useEditorLog;
    public int channelLimit = 2048; //Limits the number of channels we scan through for MIDIDMX, so full range scans are kept to a minimum.

    public enum Status : int
    {
        Disconnected, //Not connected to MIDI
        ConnectedWait, //Connected to MIDI, not connected to world.
        ConnectedSendingData, //Connected to world and sending data
    }

    const int maxChannels = 16384;
    const int channelsPerUpdate = 100; //KEEP THIS AT 100 until VRC fixes their buffers :)
    const int idleScanChannels = 10; //How many channels to send at a time during idle scans. Keep this low so we have bandwidth for actively changing channels.

    byte[] midiData = new byte[maxChannels];

    int bankStatus = 0;
    OutputDevice midiOutput;
    FileStream logStream;

    int midiUpdates = 0;
    int midiCatchup = 0;
    int midiScanPosition = 0;

    long midiLastUpdate = 0;

    /// <summary>
    /// Gets list of MIDI devices
    /// </summary>
    /// <returns>String List of devices</returns>
    public ICollection<string> GetMidiDevices()
    {
        ICollection<string> tDevices = new List<string> { "(none)" };
        ICollection<OutputDevice> devices = OutputDevice.GetAll();

        foreach (OutputDevice device in devices)
        {
            tDevices.Add(device.Name);
        }

        return tDevices;
    }

    /// <summary>
    /// Sets active midi device, connects immediately.
    /// </summary>
    /// <param name="device">String name of device to connect to.</param>
    /// <returns>True if connected, false on any failure.</returns>
    public bool MidiConnectDevice(string device)
    {
        try
        {
            midiOutput = OutputDevice.GetByName(device);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the status of the Midi Exporter
    /// </summary>
    /// <returns>
    /// Returns a MIDIDMX.Status indicating the status.
    /// 
    /// Status.Disconnected - No MIDI connection.
    /// Status.ConnectedWait - Connected to MIDI, waiting on a compatible world to connect to.
    /// Status.ConnectedSendingData - Connected to MIDI and actively sending data to a compatible world.
    /// </returns>
    public Status MidiStatus()
    {
        float midiTimeout = (float)(Stopwatch.GetTimestamp() - midiLastUpdate) / (float)Stopwatch.Frequency;

        if (midiOutput == null)
        {
            return Status.Disconnected;
        }
        else if (midiTimeout > 1)
        {
            return Status.ConnectedWait;
        }
        else
        {
            return Status.ConnectedSendingData;
        }
    }

    /// <summary>
    /// Serializes a channel from a raw byte representation
    /// </summary>
    /// <param name="channelValue"></param>
    /// <param name="channel"></param>
    public void SerializeChannel(byte channelValue, int channel)
    {
    }

    /// <summary>
    /// Called at the start of each frame to reset any state.
    /// </summary>
    public void InitFrame()
    {
    }

    /// <summary>
    /// Called after all channels have been serialized for the current frame.
    /// Can be used to for example generate a CRC block area, or operate on multiple channels at once.
    /// </summary>
    /// <param name="pixels"></param>
    /// <param name="channelValues"></param>
    public void CompleteFrame(ref List<byte> channelValues)
    {
        if (isMidiReady())
        {
            //Finds all channels that need data sent over
            //Sends only up to 
            midiUpdates = 0;
            for (int i = midiCatchup; i < channelValues.Count; i++)
            {
                if ((channelValues[i] != midiData[i] || (i >= midiScanPosition && i < midiScanPosition + idleScanChannels)) && i < channelLimit)
                {
                    if (midiUpdates >= channelsPerUpdate)
                    {
                        midiCatchup = i;
                        break;
                    }
                    midiData[i] = channelValues[i];

                    SendMidi(i, channelValues[i]);
                }
            }

            if (midiUpdates < channelsPerUpdate)
            {
                midiCatchup = 0;
            }

            midiScanPosition += idleScanChannels;
            if (midiScanPosition > channelLimit)
            {
                midiScanPosition = 0;
            }

            midiWatchdog();
            midiLastUpdate = Stopwatch.GetTimestamp();
        }
        else
        {
            float midiTimeout = (float)(Stopwatch.GetTimestamp() - midiLastUpdate) / (float)Stopwatch.Frequency;
            if (midiTimeout > 1)
            {
                Reset();

                midiLastUpdate = Stopwatch.GetTimestamp();
            }
        }
    }

    /// <summary>
    /// Called to initialize the exporter
    /// </summary>
    public void Construct()
    {
        Reset();
    }

    /// <summary>
    /// 
    /// </summary>
    public void Deconstruct()
    {
        if (midiOutput != null)
            midiOutput.Dispose();

        if (logStream != null)
            logStream.Close();
    }

    /// <summary>
    /// Resets entire midi chain, clearing all data and beginning world knocking again.
    /// </summary>
    private void Reset()
    {
        midiData = new byte[maxChannels];
        midiUpdates = 0;
        midiScanPosition = 0;
        midiCatchup = 0;

        findVRCLog();
        ChangeBanks(0);
        midiKnock();
        midiWatchdog();
    }

    /// <summary>
    /// Sends a DMX channel as MIDI
    /// </summary>
    /// <param name="channel">DMX channel number with universe added</param>
    /// <param name="value">DMX value</param>
    private void SendMidi(int channel, byte value)
    {
        if (midiOutput == null) return;

        int bank = (int)(channel / 2048);
        if (bankStatus != bank)
        {
            ChangeBanks(bank);
        }

        channel -= bank * 2048;

        if (channel < 1024)
        {
            int t = channel;
            NoteOnEvent noteOn = new NoteOnEvent();
            noteOn.Channel = (FourBitNumber)((t >> 6) & 0xF);
            noteOn.NoteNumber = (SevenBitNumber)(((t << 1) & 0x7F) + ((value >> 7) & 0x1));
            noteOn.Velocity = (SevenBitNumber)(value & 0x7F);
            midiOutput.SendEvent(noteOn);
        }
        else
        {
            int t = channel - 1024;
            NoteOffEvent noteOff = new NoteOffEvent();
            noteOff.Channel = (FourBitNumber)((t >> 6) & 0xF);
            noteOff.NoteNumber = (SevenBitNumber)(((t << 1) & 0x7F) + ((value >> 7) & 0x1));
            noteOff.Velocity = (SevenBitNumber)(value & 0x7F);
            midiOutput.SendEvent(noteOff);
        }
        midiUpdates++;
    }

    /// <summary>
    /// Sends a MIDI control signal
    /// </summary>
    /// <param name="channel">Channel, usually 15 for MIDIDMX</param>
    /// <param name="control">Control number/note number</param>
    /// <param name="value">Value</param>
    private void SendMidiControl(int channel, int control, int value)
    {
        if (midiOutput == null) return;

        ControlChangeEvent midiWD = new ControlChangeEvent();
        midiWD.Channel = (FourBitNumber)channel;
        midiWD.ControlNumber = (SevenBitNumber)control;
        midiWD.ControlValue = (SevenBitNumber)value;

        midiOutput.SendEvent(midiWD);
    }

    /// <summary>
    /// Changes data banks.
    /// A data bank is a set of 2048 channels.
    /// </summary>
    /// <param name="bank">Bank number from 0 to 7.</param>
    private void ChangeBanks(int bank)
    {
        bankStatus = bank;

        if (bank < 0) bank = 0;
        if (bank > 7) bank = 7;

        SendMidiControl(15, 127, bank);
        midiUpdates++;
    }

    /// <summary>
    /// Checks if we're good to process and send MIDI data.
    /// This reads the VRC log file to ensure the world responded to our watchdog signal.
    /// A watchdog packet MUST be sent before checking this.
    /// </summary>
    /// <returns>True if ready for more data, false if we're not.</returns>
    private bool isMidiReady()
    {
        if (logStream == null)
        {
            findVRCLog();
            return false;
        }

        if (midiOutput == null)
        {
            return false;
        }

        int length = (int)(logStream.Length - logStream.Position);

        //there's a better way for this but I'm sleepy and don't feel like parsing through C# documentation
        //listen I write too many languages as it is ok
        byte[] searchWord = { (byte)'M', (byte)'I', (byte)'D', (byte)'I', (byte)'R', (byte)'E', (byte)'A', (byte)'D', (byte)'Y', };

        if (length > 1)
        {
            int c;
            int i = 0;
            while ((c = logStream.ReadByte()) != -1)
            {
                if (c == searchWord[i])
                {
                    i++;

                    if (i >= searchWord.Length)
                    {
                        logStream.Position = logStream.Length - 1;
                        return true;
                    }
                }
            }
        }
        else
        {
            return false;
        }

        return false;
    }

    /// <summary>
    /// Sends watchdog packet to the world.
    /// </summary>
    private void midiWatchdog()
    {
        SendMidiControl(15, 127, 127);
    }

    /// <summary>
    /// Sends a sequence of commands to 'knock' the world receiver into accepting data.
    /// MIDIDMX on the world side may not respond until this is done.
    /// </summary>
    private void midiKnock()
    {
        SendMidiControl(15, 127, 101);
        SendMidiControl(15, 127, 120);
        SendMidiControl(15, 127, 107);
    }

    /// <summary>
    /// Finds the VRC log to watch for watchdog signals.
    /// This will close the stream and find the latest log available if called again.
    /// </summary>
    private void findVRCLog()
    {
        if (logStream != null)
        {
            logStream.Close();
            logStream = null;
        }

        string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string[] logs = Directory.GetFiles(path + "\\..\\LocalLow\\VRChat\\VRChat", "output_log_*.txt", SearchOption.TopDirectoryOnly);
        if (logs.Length == 0) return;

        Array.Sort(logs);
        string log = logs[logs.Length - 1];

        //Editor!!
        if (useEditorLog)
        {
            log = path + "\\..\\Local\\Unity\\Editor\\Editor.log";
        }

        logStream = new FileStream(log, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        //forward to the end to wait on it
        logStream.Position = logStream.Length - 1;
    }
}