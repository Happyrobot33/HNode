using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using SharpCompress;
using SharpCompress.Writers;
using System.IO;
using System.IO.Compression;
using SharpCompress.Compressors.LZMA;
using System.Buffers;
using System.Linq;
using SharpCompress.Compressors.Deflate;
using SharpCompress.IO;

public class Compressor : IDMXGenerator
{
    public void Construct()
    {
    }

    public void ConstructUserInterface(RectTransform rect)
    {
    }

    public void Deconstruct()
    {
    }

    public void DeconstructUserInterface()
    {
    }

    public void GenerateDMX(ref List<byte> dmxData)
    {
        //ZLib
        using MemoryStream inputStream = new MemoryStream(dmxData.ToArray());
        using MemoryStream outputStream = new();
        using ZlibStream zlibStream = new ZlibStream(SharpCompressStream.Create(outputStream, leaveOpen: true), SharpCompress.Compressors.CompressionMode.Compress);
        zlibStream.FlushMode = FlushType.Sync;
        inputStream.CopyTo(zlibStream);

        outputStream.Seek(0, SeekOrigin.Begin);
        dmxData = new List<byte>(outputStream.ToArray());
        zlibStream.Close();

        //LZMA
        /* using MemoryStream inputStream = new MemoryStream(dmxData.ToArray());
        using MemoryStream outputStream = new();
        using LzmaStream lzmaStream = new LzmaStream(LzmaEncoderProperties.Default, false, outputStream);
        inputStream.CopyTo(lzmaStream);
        lzmaStream.Close();

        //use the compressed data
        outputStream.Seek(0, SeekOrigin.Begin);
        int sizeBefore = dmxData.Count;
        dmxData = new List<byte>(outputStream.ToArray());
        int sizeAfter = dmxData.Count;
        float ratio = 1 - (float)sizeAfter / (float)sizeBefore;
        //format as percentage
        Debug.Log(ratio.ToString("P2"));

        /* //now confirm it matches by decompressing
        var uncompressedOutput = new MemoryStream();
        DecompressLzmaStream(lzmaStream.Properties, outputStream, sizeAfter, uncompressedOutput, sizeBefore);

        //check if the same
        Debug.Log(Enumerable.SequenceEqual(uncompressedOutput.ToArray(), inputStream.ToArray()) ? "Decompression successful" : "Decompression failed");

        uncompressedOutput.Seek(0, SeekOrigin.Begin);
        dmxData = new List<byte>(uncompressedOutput.ToArray()); */
    }
    
    private static void DecompressLzmaStream(byte[] properties, Stream compressedStream, long compressedSize, Stream decompressedStream, long decompressedSize)
    {
        LzmaStream lzmaStream = new LzmaStream(properties, compressedStream, compressedSize, -1, null, false);

        byte[] buffer = ArrayPool<byte>.Shared.Rent(1024);
        long totalRead = 0;
        while (totalRead < decompressedSize)
        {
            int toRead = (int)Math.Min(buffer.Length, decompressedSize - totalRead);
            int read = lzmaStream.Read(buffer, 0, toRead);
            if (read > 0)
            {
                decompressedStream.Write(buffer, 0, read);
                totalRead += read;
            }
            else
            {
                break;
            }
        }
        ArrayPool<byte>.Shared.Return(buffer);
    }

    public void UpdateUserInterface()
    {
    }
}
