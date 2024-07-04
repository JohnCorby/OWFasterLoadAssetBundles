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

        int b;
        var i = 0;
        while ((b = stream.ReadByte()) != -1 && i < c_BufferSize)
        {
            i += 2;
            hash.Append(b.ToString("X2"));
        }

        OWFasterLoadAssetBundles.Instance.ModHelper.Console.WriteLine("done hashing stream", MessageType.Info);

        return BitConverter.GetBytes(hash.ToString().GetHashCode());
    }

    public static string HashToString(Span<byte> hash)
    {
        OWFasterLoadAssetBundles.Instance.ModHelper.Console.WriteLine("Hash to string", MessageType.Info);

        Span<char> chars = stackalloc char[hash.Length * 2];

        for (var i = 0; i < hash.Length; i++)
        {
            var b = hash[i];
            var s = b.ToString("X2").ToCharArray();
            chars[i * 2] = s[0];
            chars[i * 2 + 1] = s[1];
            OWFasterLoadAssetBundles.Instance.ModHelper.Console.WriteLine($"{s}", MessageType.Info);

        }
        OWFasterLoadAssetBundles.Instance.ModHelper.Console.WriteLine($"oh boy im done also the length is {hash.Length}", MessageType.Info);


        return new string(chars.ToArray());
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
            var s = new string(hash.Skip(i).Take(2).ToArray());
            destination[i / 2] = byte.Parse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        return hash.Length / 2;
    }
}
