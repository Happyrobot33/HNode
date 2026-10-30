using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays a color square in its configuration card based on the values of the R, G and B DMX channels.
/// </summary>
public class ColorPreviewExporter : IExporter
{
    public DMXChannel red = 0;
    public DMXChannel green = 1;
    public DMXChannel blue = 2;

    private Color32 currentColor = new Color32(0, 0, 0, 255);
    private Image previewImage;

    public void Construct() {}
    public void Deconstruct() {}
    public void InitFrame(ref List<byte> channelValues) {}
    public void SerializeChannel(byte channelValue, int channel) {}
    public void FrameRendered(ref Texture2D texture) {}

    public void CompleteFrame(ref List<byte> channelValues)
    {
        currentColor = new Color32(
            GetValue(channelValues, red),
            GetValue(channelValues, green),
            GetValue(channelValues, blue),
            255);
    }

    private static byte GetValue(List<byte> values, int channel)
    {
        return channel >= 0 && channel < values.Count ? values[channel] : (byte)0;
    }

    private void AddChannelField(RectTransform rect, string title, DMXChannel value, Action<DMXChannel> setter)
    {
        Util.AddInputField(rect, title)
            .WithText(value)
            .WithCallback((text) =>
            {
                try
                {
                    setter(text);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Invalid DMX channel '{text}': {ex.Message}");
                }
            });
    }

    public void ConstructUserInterface(RectTransform rect)
    {
        AddChannelField(rect, "Red Channel", red, v => red = v);
        AddChannelField(rect, "Green Channel", green, v => green = v);
        AddChannelField(rect, "Blue Channel", blue, v => blue = v);

        var squareObject = new GameObject("ColorSquare");
        squareObject.transform.SetParent(rect, false);
        previewImage = squareObject.AddComponent<Image>();
        previewImage.color = currentColor;
        ((RectTransform)squareObject.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 100f);
    }

    public void DeconstructUserInterface()
    {
        previewImage = null;
    }

    public void UpdateUserInterface()
    {
        if (previewImage != null)
        {
            previewImage.color = currentColor;
        }
    }
}
