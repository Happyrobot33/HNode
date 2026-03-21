using ArtNet;
using Klak.Spout;
using UnityEngine;

//this should really be turned into a IDMXGenerator
public class TextureReader : MonoBehaviour
{
    public static int TextureWidth = 1920;
    public static int TextureHeight = 1080;
    public byte[] dmxData;
    public Loader Loader;
    public RenderTexture texture;
    public Texture2D texture2D;
    public SpoutReceiver spoutReceiver;

    void Start()
    {
        texture2D = new Texture2D(TextureWidth, TextureHeight, TextureFormat.BGRA32, false);
    }

    public void ChangeResolution(Resolution resolution)
    {
        texture2D.Reinitialize(resolution.width, resolution.height);
        TextureWidth = resolution.width;
        TextureHeight = resolution.height;
    }

    void Update()
    {
        if (!Loader.showconf.Transcode)
        {
            //disable receiver
            spoutReceiver.enabled = false;
            return;
        }

        //enable receiver
        spoutReceiver.enabled = true;

        dmxData = new byte[Loader.showconf.TranscodeUniverseCount * 512];

        //set RT active
        RenderTexture.active = texture;
        texture2D.ReadPixels(new Rect(0, 0, TextureWidth, TextureHeight), 0, 0);
        RenderTexture.active = null;

        for (int i = 0; i < Loader.showconf.TranscodeUniverseCount * 512; i++)
        {
            Loader.showconf.Deserializer.DeserializeChannel(texture2D, ref dmxData[i], i, TextureWidth, TextureHeight);
        }
    }

    public static Color GetColor(Texture2D tex, int x, int y)
    {
        //invert y
        y = TextureHeight - 1 - y;
        return tex.GetPixel(x, y).gamma;
    }
}
