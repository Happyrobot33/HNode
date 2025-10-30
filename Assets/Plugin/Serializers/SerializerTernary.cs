using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public class Ternary : IDMXSerializer
{
    const int blockSize = 4; // 10x10 pixels per channel block
    const int blocksPerCol = 8 * 6; // channels per column

    public void Construct() { }
    public void Deconstruct() { }
    public void InitFrame(ref List<byte> channelValues) { }
    public void CompleteFrame(ref Color32[] pixels, ref List<byte> channelValues, int textureWidth, int textureHeight) { }

    public void SerializeChannel(ref Color32[] pixels, byte channelValue, int channel, int textureWidth, int textureHeight)
    {
        //split the value into 6 ternary bits
        var bits = convertToTernary(channelValue);

        for (int i = 0; i < bits.Length; i++)
        {
            GetPositionData(channel, i, out int x, out int y);
            if (x >= textureWidth || y >= textureHeight)
            {
                continue; // Skip if the calculated pixel is out of bounds
            }
            //remap the value to intensity
            float t = Mathf.InverseLerp(0, 2, bits[i]);
            byte intensity = (byte)Mathf.Lerp(0, byte.MaxValue, t);
            //convert the x y to pixel index
            //return 4x4 area
            var color = new Color32(
                intensity,
                intensity,
                intensity,
                Util.GetBlockAlpha(channelValue)
            );
            TextureWriter.MakeColorBlock(ref pixels, x, y, color, blockSize);
        }
    }

    public void DeserializeChannel(Texture2D tex, ref byte channelValue, int channel, int textureWidth, int textureHeight)
    {
        //NOT IMPLEMENTED PROPERLY YET
        var bits = new BitArray(8);
        for (int i = 0; i < bits.Length; i++)
        {
            GetPositionData(channel, i, out int x, out int y);
            //add on a offset
            x += 1;
            y += 1;
            if (x >= textureWidth || y >= textureHeight)
            {
                continue; // Skip if the calculated pixel is out of bounds
            }
            // Read the 4x4 area and combine it into a single byte
            bits[i] = TextureReader.GetColor(tex, x, y).r > 0.5f;
        }
        // Convert the BitArray back to a byte
        channelValue = ConvertToByte(bits);
    }

    private static void GetPositionData(int channel, int i, out int x, out int y)
    {
        int newChannel = (channel * 6) + i;
        x = (newChannel / blocksPerCol) * blockSize;
        y = (newChannel % blocksPerCol) * blockSize;
    }

    byte ConvertToByte(BitArray bits)
    {
        if (bits.Count != 8)
        {
            throw new ArgumentException("bits");
        }
        byte[] bytes = new byte[1];
        bits.CopyTo(bytes, 0);
        return bytes[0];
    }

    /// <summary>
    /// Convert to ternary representation
    /// </summary>
    /// <param name="N"></param>
    /// <returns></returns>
    public byte[] convertToTernary(byte N)
    {
        // Base case
        if (N == 0)
            return new byte[6];

        byte[] bytes = new byte[6];
        int index = 5;
        for (int i = 0; i < 6; i++)
        {
            bytes[index--] = (byte)(N % 3);
            N /= 3;
        }

        return bytes;
    }

    public void ConstructUserInterface(RectTransform rect)
    {

    }

    public void DeconstructUserInterface()
    {

    }

    public void UpdateUserInterface()
    {

    }
}
