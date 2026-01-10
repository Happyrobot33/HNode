using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RemapOnDemand : IDMXGenerator
{
    public DMXChannel ToggleChannel = 0;
    public DMXChannel RemapFromChannelStart = 0;
    public DMXChannel RemapToChannelStart = 0;
    public EquationNumber RemapChannelLength = 0;
    public EquationNumber ActivationThreshold = 127;
    
    public void Construct() { }
    public void Deconstruct() { }
    public void GenerateDMX(ref List<byte> dmxData)
    {
        if (dmxData[ToggleChannel] > ActivationThreshold)
        {
            dmxData.WriteToListAtPosition(dmxData.GetRange(RemapFromChannelStart, RemapChannelLength), RemapToChannelStart);
        }
    }

    private protected TMP_InputField toggleChannelInputfield;
    private protected TMP_InputField remapFromChannelStartInputfield;
    private protected TMP_InputField remapToChannelStartInputfield;
    private protected TMP_InputField remapChannelLengthInputfield;
    private protected TMP_InputField activationThresholdInputfield;
    
    public void ConstructUserInterface(RectTransform rect)
    {
        toggleChannelInputfield = Util.AddInputField(rect, "Toggle Channel");
        toggleChannelInputfield.text = ToggleChannel.ToString();
        toggleChannelInputfield.onEndEdit.AddListener((value) =>
        {
            ToggleChannel = value;
        });
        
        remapFromChannelStartInputfield = Util.AddInputField(rect, "Remap From Channel Start");
        remapFromChannelStartInputfield.text = RemapFromChannelStart.ToString();
        remapFromChannelStartInputfield.onEndEdit.AddListener((value) =>
        {
            RemapFromChannelStart = value;
        });
        
        remapToChannelStartInputfield = Util.AddInputField(rect, "Remap To Channel Start");
        remapToChannelStartInputfield.text = RemapToChannelStart.ToString();
        remapToChannelStartInputfield.onEndEdit.AddListener((value) =>
        {
            RemapToChannelStart = value;
        });
        
        remapChannelLengthInputfield = Util.AddInputField(rect, "Remap Channel Length");
        remapChannelLengthInputfield.text = RemapChannelLength.ToString();
        remapChannelLengthInputfield.onEndEdit.AddListener((value) =>
        {
            RemapChannelLength = value;
        });
        
        activationThresholdInputfield = Util.AddInputField(rect, "Activation Threshold");
        activationThresholdInputfield.text = ActivationThreshold.ToString();
        activationThresholdInputfield.onEndEdit.AddListener((value) =>
        {
            ActivationThreshold = value;
        });
    }

    public void DeconstructUserInterface() { }
    public void UpdateUserInterface() { }

}
