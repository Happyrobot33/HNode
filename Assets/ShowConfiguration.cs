using System.Collections.Generic;
using System.Net;
using UnityEngine;
using YamlDotNet.Serialization;

public class ShowConfiguration
{
    //these are both saved as part of a show configuration, and as part of player prefs
    public IDMXSerializer Serializer;
    public IDMXSerializer Deserializer;
    public List<IDMXGenerator> Generators;
    public List<IExporter> Exporters;


    [YamlMember(Description = "Whether to enable transcoding from the deserializer to the serializer. This is useful for converting between different pixel mapping formats.")]
    public bool Transcode { get; set; }
    [YamlMember(Description = "The number of universes to do transcoding for. This is useful for limiting the amount of data being processed if you know you only need a certain number of universes.")]
    public int TranscodeUniverseCount { get; set; } = 3;
    [YamlMember(Description = "The maximum number of universes that will be serialized. This usually doesnt need to be changed")]
    public int SerializeUniverseCount { get; set; } = int.MaxValue; //this is the maximum number of universes that can be used for serializing.
    [YamlMember(Description = "A list of channels to mask out. These channels will be forced to transparent. Define a start and end channel for each mask.")]
    public List<DMXChannelRange> maskedChannels { get; set; }
    /// <summary>
    /// If true, the mask will be inverted, meaning that the channels that channels set in <see cref="maskedChannels"/> will be the only ones visible.
    /// </summary>
    [YamlMember(Description = "If true, the mask will be inverted, making the channels that are set in the maskedChannels list the only ones visible.")]
    public bool invertMask { get; set; }
    /// <summary>
    /// If true, the mask will automatically be applied to channels that are set to zero.
    /// </summary>
    [YamlMember(Description = "If true, the mask will automatically be applied to channels that are set to zero, making them transparent.")]
    public bool autoMaskOnZero { get; set; }

    //public string ArtnetIP { get; set; } = "127.0.0.1"; //disabled for now as this is not natively supported by the library yet?
    //public int ArtnetPort { get; set; } = 6454;
    public string SpoutInputName { get; set; } = "HNode Input";
    public string SpoutOutputName { get; set; } = "HNode Output";
    public int ArtNetPort { get; set; } = 6454;
    [YamlMember(Description = "The address to listen to for artnet information. Set this to 0.0.0.0 to automatically find artnet information across all network interfaces")]
    public SerializableIPAddress ArtNetAddress { get; set; } = IPAddress.Any;
    public int TargetFramerate { get; set; } = 60;
    [YamlMember(Description = "The background Chroma Key color to use on the F11 Preview pane, RGBA (0 - 255) Integer")]
    public Color32 PreviewChromaColor { get; set; } = new Color32(0, 255, 0, 0);
    public Resolution OutputResolution { get; set; } = new Resolution(1920, 1080);
    public Resolution InputResolution { get; set;} = new Resolution(1920, 1080);

    //initializer
    public ShowConfiguration()
    {
        maskedChannels = new List<DMXChannelRange>();
        Generators = new List<IDMXGenerator>();
        Exporters = new List<IExporter>();
    }
}
