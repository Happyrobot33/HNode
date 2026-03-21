using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Snapshot : IDMXGenerator
{
    public DMXChannel channelStart = 0;
    public DMXChannel channelEnd = 1;

    private byte[] snapshotData = null;
    private bool takeSnapshotNextFrame = false;

    public virtual void Construct() { }
    public virtual void Deconstruct() { }

    public virtual void GenerateDMX(ref List<byte> dmxData)
    {
        if (takeSnapshotNextFrame)
        {
            takeSnapshotNextFrame = false;
            int count = channelEnd - channelStart + 1;
            snapshotData = new byte[count];
            for (int i = 0; i < count; i++)
            {
                int channel = channelStart + i;
                snapshotData[i] = channel < dmxData.Count ? dmxData[channel] : (byte)0;
            }
        }

        if (snapshotData == null) return;

        //write the stored snapshot values back into dmxData
        for (int i = 0; i < snapshotData.Length; i++)
        {
            int channel = channelStart + i;
            if (channel < dmxData.Count)
                dmxData[channel] = snapshotData[i];
        }
    }

    private protected TMP_InputField channelStartInputfield;
    private protected TMP_InputField channelEndInputfield;
    public virtual void ConstructUserInterface(RectTransform rect)
    {
        channelStartInputfield = Util.AddInputField(rect, "Channel Start")
            .WithText(channelStart)
            .WithCallback((value) => { channelStart = value; });

        channelEndInputfield = Util.AddInputField(rect, "Channel End")
            .WithText(channelEnd)
            .WithCallback((value) => { channelEnd = value; });

        var takeSnapshotButton = Util.AddButton(rect, "Take Snapshot");
        takeSnapshotButton.onClick.AddListener(() =>
        {
            takeSnapshotNextFrame = true;
        });

        var clearSnapshotButton = Util.AddButton(rect, "Clear Snapshot");
        clearSnapshotButton.onClick.AddListener(() =>
        {
            snapshotData = null;
        });
    }

    public void DeconstructUserInterface()
    {
        //throw new NotImplementedException();
    }

    public void UpdateUserInterface()
    {

    }
}
