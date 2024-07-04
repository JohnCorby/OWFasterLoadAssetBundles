using OWML.Utils;
using System;
using System.Buffers.Binary;
using System.Globalization;
using System.IO;
using Unity.Collections;
using UnityEngine;
using System.Linq;
using System.Text;
using OWML.Common;

namespace OWFasterLoadAssetBundles.Helpers;
internal class HashingHelper
{
    private const int c_BufferSize = 4096;

    public static byte[] HashFile(string path)
    {
        OWFasterLoadAssetBundles.Instance.ModHelper.Console.WriteLine("Hashing file", MessageType.Info);

        using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None, c_BufferSize, FileOptions.SequentialScan);
        return HashStream(fileStream);
    }

    public static byte[] HashStream(Stream stream)
    {
        OWFasterLoadAssetBundles.Instance.ModHelper.Console.WriteLine("Hashing stream", MessageType.Info);

        stream.Seek(0, SeekOrigin.Begin);

        var hash = new StringBuilder();

        var buffer = new byte[4];
        while (stream.Read(buffer, 0, 4) > 0)
        {
            hash.Append(buffer);
        }

        var hashArray = new byte[16];
        BinaryPrimitives.WriteUInt64LittleEndian(hashArray, hash.GetValue<ulong>("u64_0"));
        BinaryPrimitives.WriteUInt64LittleEndian(hashArray.Skip(8).ToArray().AsSpan(), hash.GetValue<ulong>("u64_1"));

        return hashArray;
    }

    public static string HashToString(Span<byte> hash)
    {
        OWFasterLoadAssetBundles.Instance.ModHelper.Console.WriteLine("Hash to string", MessageType.Info);

        return BitConverter.ToString(hash.ToArray()).Replace("-", "");
    }

    public static int WriteHash(Span<byte> destination, string hash)
    {
        OWFasterLoadAssetBundles.Instance.ModHelper.Console.WriteLine("Writing hash", MessageType.Info);

        if ((hash.Length / 2) > destination.Length)
        {
            throw new ArgumentOutOfRangeException("Destination is small to write hash", nameof(destination));
        }

        for (var i = 0; i < hash.Length; i += 2)
        {
            var s = hash.Skip(i).Take(2).ToArray().ToString();
            destination[i / 2] = byte.Parse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        return hash.Length / 2;
    }
}
