using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ArtNet;
using Klak.Spout;
using TMPro;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;
using ByteSizeLib;

public class TextureWriter : MonoBehaviour
{
    public DmxManager dmxManager;
    public TextureReader reader;
    public Texture2D texture;
    public const int TextureWidth = 1920;
    public const int TextureHeight = 1080;
    public SpoutSender spoutSender;

    private System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
    public TextMeshProUGUI frameTime;
    public Material mat;
    public Material previewMat;
    private Color32[] pixels;

    void Start()
    {
        //find the Loader
        //Loader = FindAnyObjectByType<Loader>();

        //maskedChannels.AddRange(Enumerable.Range(0, 25));
        //maskedChannels.Add(52);
        //maskedChannels.Add(102);

        texture = new Texture2D(TextureWidth, TextureHeight, TextureFormat.RGBA32, false, true);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        spoutSender.sourceTexture = texture;
        mat.mainTexture = texture;
        previewMat.mainTexture = texture;

        pixels = new Color32[TextureWidth * TextureHeight];
    }

    // Update is called once per frame
    void Update()
    {
        //start a profiler timer
        timer.Restart();

        Profiler.BeginSample("Texture Clear");
        //fill with transparent
        var color = new Color32(0, 0, 0, 0);
        Array.Fill(pixels, color);
        Profiler.EndSample();

        Profiler.BeginSample("DMX Merge");
        List<byte> mergedDmxValues = new List<byte>();
        if (Loader.showconf.Transcode)
        {
            mergedDmxValues = reader.dmxData.ToList();
        }
        else
        {
            var universeCount = dmxManager.Universes().Length;


            //merge all universes into one byte array
            for (ushort u = 0; u < universeCount; u++)
            {
                byte[] dmxValues = dmxManager.DmxValues(u);
                mergedDmxValues.AddRange(dmxValues);
            }
        }

        Profiler.EndSample();
        Profiler.BeginSample("DMX Generators");
        //now run the generators in order
        foreach (var generator in Loader.showconf.Generators)
        {
            generator.GenerateDMX(ref mergedDmxValues);
        }
        Profiler.EndSample();

        Loader.showconf.Serializer.InitFrame(ref mergedDmxValues);
        foreach (var exporter in Loader.showconf.Exporters)
        {
            exporter.InitFrame(ref mergedDmxValues);
        }

        Profiler.BeginSample("Serializer Loop");
        var ChannelsToSerialize = Math.Min((long)Loader.showconf.SerializeUniverseCount * 512, mergedDmxValues.Count);
        for (int i = 0; i < ChannelsToSerialize; i++)
        {
            //check if between any masked channel sets
            bool isMasked = false;
            foreach (var channel in Loader.showconf.maskedChannels)
            {
                if (channel.Contains(i))
                {
                    isMasked = true;
                    break;
                }
            }

            if (Loader.showconf.invertMask)
            {
                isMasked = !isMasked;
            }

            //skip the channel if its masked
            if (isMasked)
            {
                continue;
            }

            Profiler.BeginSample("Individual Channel Serialization");
            Loader.showconf.Serializer.SerializeChannel(ref pixels, mergedDmxValues[i], i, TextureWidth, TextureHeight);
            foreach (var exporter in Loader.showconf.Exporters)
            {
                exporter.SerializeChannel(mergedDmxValues[i], i);
            }
            Profiler.EndSample();
        }
        Profiler.EndSample();

        Profiler.BeginSample("Frame Finalization");
        Loader.showconf.Serializer.CompleteFrame(ref pixels, ref mergedDmxValues, TextureWidth, TextureHeight);
        foreach (var exporter in Loader.showconf.Exporters)
        {
            exporter.CompleteFrame(ref mergedDmxValues);
        }
        Profiler.EndSample();

        //send to the UV Remapper

        Profiler.BeginSample("Texture Write");
        texture.SetPixels32(pixels);
        texture.Apply();
        Profiler.EndSample();

        timer.Stop();

        frameTime.text = $"Serialization Time: {timer.ElapsedMilliseconds} ms";
        frameTime.text += $"\nDMX Channels: {mergedDmxValues.Count}";
        //figure out bytes per second
        var bytesPerSecond = mergedDmxValues.Count / UnityEngine.Time.smoothDeltaTime;
        var prettyBytes = ByteSize.FromBytes(bytesPerSecond).ToString("0.##");
        frameTime.text += $"\nData Throughput: {prettyBytes}/s";
    }

    public static int PixelToIndex(int x, int y)
    {
        //check if its in bounds, and return -1 if not
        if (x < 0 || x >= TextureWidth || y < 0 || y >= TextureHeight)
        {
            return -1;
        }

        //make sure y is flipped
        y = TextureHeight - 1 - y;
        return y * TextureWidth + x;
    }

    public static void MakeColorBlock(ref Color32[] pixels, int x, int y, Color32 color, int size)
    {
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                int index = PixelToIndex(x + i, y + j);
                if (index == -1) return;
                pixels[index] = color;
            }
        }
    }

    public static void MixColorBlock(ref Color32[] pixels, int x, int y, byte channelValue, ColorChannel channel, int size)
    {
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                int index = PixelToIndex(x + i, y + j);
                if (index == -1) return;
                if (index >= 0 && index < pixels.Length)
                {
                    //get the pixel color
                    Color32 pixelColor = pixels[index];

                    //assign just to the channel
                    switch (channel)
                    {
                        case ColorChannel.Red:
                            pixelColor.r = channelValue;
                            break;
                        case ColorChannel.Green:
                            pixelColor.g = channelValue;
                            break;
                        case ColorChannel.Blue:
                            pixelColor.b = channelValue;
                            break;
                    }
                    pixelColor.a = 255;
                    pixels[index] = pixelColor;
                }
            }
        }
    }

    public enum ColorChannel
    {
        Red,
        Green,
        Blue
    }
}
