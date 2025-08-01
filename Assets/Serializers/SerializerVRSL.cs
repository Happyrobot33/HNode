using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VRSL : IDMXSerializer
{
    public enum OutputConfigs
    {
        HorizontalTop,
        VerticalLeft,
        VerticalRight,
        HorizontalBottom,
    }
    const int blockSize = 16; // 10x10 pixels per channel block
    const int blocksPerCol = 13; // channels per column
    public bool OutputGamma = true;
    public bool InputGamma = true;
    public OutputConfigs outputConfig = OutputConfigs.HorizontalTop;
    public void Construct() { }
    public void InitFrame() { }
    public void CompleteFrame(ref Color32[] pixels, ref List<byte> channelValues, int textureWidth, int textureHeight) { }

    public void SerializeChannel(ref Color32[] pixels, byte channelValue, int channel, int textureWidth, int textureHeight)
    {
        GetPositionData(channel, out int x, out int y, out int universeOffset);

        //if vertical, flip
        switch (outputConfig)
        {
            case OutputConfigs.HorizontalTop:
                x += universeOffset;
                break;
            case OutputConfigs.HorizontalBottom:
                x += universeOffset;
                y += textureHeight - (blocksPerCol * blockSize); // Shift down for horizontal bottom layout
                break;
            case OutputConfigs.VerticalLeft:
                //swap x and y
                int temp = x;
                x = y;
                y = temp;
                y += universeOffset;
                //flip Y coordinate
                y = textureHeight - y - blockSize; // Flip Y coordinate for vertical layout
                break;
            case OutputConfigs.VerticalRight:
                //swap x and y
                temp = x;
                x = y;
                y = temp;
                y += universeOffset;
                //flip Y coordinate
                y = textureHeight - y - blockSize; // Flip Y coordinate for vertical layout
                x += textureWidth - (blocksPerCol * blockSize); // Shift to the right for vertical right layout
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(outputConfig), outputConfig, null);
        }
        

        //convert the x y to pixel index
        //return 4x4 area
        var color = new Color(
            channelValue/255f,
            channelValue/255f,
            channelValue/255f,
            Util.GetBlockAlpha(channelValue)
        );
        if (OutputGamma) { color = color.linear; } //lol WTF VRSL, you output in a converted color space instead of native linear???????
        TextureWriter.MakeColorBlock(ref pixels, x, y, color, blockSize);
    }

    public void DeserializeChannel(Texture2D tex, ref byte channelValue, int channel, int textureWidth, int textureHeight)
    {
        GetPositionData(channel, out int x, out int y, out int universeOffset);

        //add a half offset to get the center
        x += blockSize / 2;
        y += blockSize / 2;

        // Get the color block from the texture
        Color color = TextureReader.GetColor(tex, x + universeOffset, y);
        if (InputGamma) { color = color.gamma; } //TODO: test this NEEDS MORE TESTING, seems like this actually should be off by default?????

        // Convert the color block to a channel value
        channelValue = ((Color32)color).g;
    }

    private static void GetPositionData(int channel, out int x, out int y, out int universeOffset)
    {
        int universe = channel / 512; // Assuming 512 channels per universe
        int channelInUniverse = channel % 512; // Channel within the universe

        x = (channelInUniverse / blocksPerCol) * blockSize;
        y = (channelInUniverse % blocksPerCol) * blockSize;

        //stupid universe bullshit in VRSL
        universeOffset = universe * (512 / blocksPerCol * blockSize) + (universe * blockSize);
    }
}
