﻿using Newtonsoft.Json;
using OWFasterLoadAssetBundles.Helpers;
using OWFasterLoadAssetBundles.Models;
using OWML.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OWFasterLoadAssetBundles.Managers;
internal class MetadataManager
{
    private readonly string m_MetadataFile;
    private readonly object m_Lock = new();

    private List<Metadata> m_Metadata = null!;

    public MetadataManager(string metadataFile)
    {
        m_MetadataFile = metadataFile;
        LoadFile();
    }

    public Metadata? FindMetadataByHash(byte[] hash)
    {
        Span<byte> tempHash = stackalloc byte[20];

        lock (m_Lock)
        {
            foreach (var metadata in m_Metadata)
            {
                var writtenBytes = HashingHelper.WriteHash(tempHash, metadata.OriginalAssetBundleHash);
                var writtenRange = tempHash.ToArray().Take(writtenBytes).ToArray().AsSpan();
                if (writtenRange.SequenceEqual(hash))
                {
                    return metadata;
                }
            }
        }

        return null;
    }

    public void SaveMetadata(Metadata metadata)
    {
        lock (m_Lock)
        {
            var index = m_Metadata.FindIndex(m => m.OriginalAssetBundleHash.Equals(metadata.OriginalAssetBundleHash, StringComparison.InvariantCulture));

            if (index == -1)
            {
                m_Metadata.Add(metadata);
            }
            else
            {
                m_Metadata[index] = metadata;
            }
        }

        SaveFile();
    }

    public void DeleteMetadata(Metadata metadata)
    {
        var shouldSave = false;
        lock (m_Lock)
        {
            var index = m_Metadata.FindIndex(m => m.OriginalAssetBundleHash.Equals(metadata.OriginalAssetBundleHash, StringComparison.InvariantCulture));

            if (index >= 0)
            {
                shouldSave = true;
                m_Metadata.RemoveAt(index);
            }
        }

        if (shouldSave)
        {
            SaveFile();
        }
    }

    private void LoadFile()
    {
        if (!File.Exists(m_MetadataFile))
        {
            m_Metadata = new();
            return;
        }

        try
        {
            m_Metadata = JsonConvert.DeserializeObject<List<Metadata>>(File.ReadAllText(m_MetadataFile)) ?? new();
        }
        catch (Exception ex)
        {
            OWFasterLoadAssetBundles.Instance.ModHelper.Console.WriteLine($"Failed to deserialize metadata.json file\n{ex}", MessageType.Error);
            m_Metadata = new();
            return;
        }

        if (UpgradeMetadata())
        {
            return;
        }

        DeleteOldBundles();
    }

    /// <summary>
    /// Upgrades metadata file if some changes happened in <see cref="Metadata"/>.
    /// </summary>
    /// <returns><see langword="true"/> when metadata was upgraded, otherwise <see langword="false"/></returns>
    private bool UpgradeMetadata()
    {
        var shouldSave = false;
        foreach (var metadata in m_Metadata)
        {
            var isTimeDefault = metadata.LastAccessTime == default;
            if (isTimeDefault)
            {
                metadata.LastAccessTime = DateTime.Now;
            }

            shouldSave |= isTimeDefault;
        }

        if (shouldSave)
        {
            SaveFile();
        }

        return shouldSave;
    }

    private void SaveFile()
    {
        lock (m_Lock)
        {
            File.WriteAllText(m_MetadataFile, JsonConvert.SerializeObject(m_Metadata));
        }
    }

    private void DeleteOldBundles()
    {
        const int c_DeleteAfterDays = 3;

        for (var i = m_Metadata.Count - 1; i >= 0; i--)
        {
            var metadata = m_Metadata[i];
            if ((DateTime.Now - metadata.LastAccessTime).TotalDays < c_DeleteAfterDays)
            {
                continue;
            }

            m_Metadata.RemoveAt(i);
            if (metadata.UncompressedAssetBundleName == null)
            {
                continue;
            }

            OWFasterLoadAssetBundles.Instance.ModHelper.Console.WriteLine($"Deleting unused asset bundle cache {metadata.UncompressedAssetBundleName}", MessageType.Info);
            Patcher.AssetBundleManager.DeleteCachedAssetBundle(Path.Combine(Patcher.AssetBundleManager.CachePath, metadata.UncompressedAssetBundleName));
        }

        // delete unknown bundles
        var deletedBundleCount = 0;
        foreach (var bundlePath in Directory.GetFiles(Patcher.AssetBundleManager.CachePath, "*.assetbundle", SearchOption.TopDirectoryOnly))
        {
            var bundleName = Path.GetFileName(bundlePath);
            var metadata = m_Metadata.Find(
                m => m.UncompressedAssetBundleName != null && m.UncompressedAssetBundleName.Equals(bundleName, StringComparison.InvariantCulture));

            if (metadata == null)
            {
                DeleteFileSafely(ref deletedBundleCount, bundlePath);
            }
        }

        if (deletedBundleCount > 0)
        {
            OWFasterLoadAssetBundles.Instance.ModHelper.Console.WriteLine($"Deleted {deletedBundleCount} unknown bundles. Metadata file got corrupted?", MessageType.Warning);
        }

        static void DeleteFileSafely(ref int counter, string path)
        {
            if (!FileHelper.TryDeleteFile(path, out var exception))
            {
                OWFasterLoadAssetBundles.Instance.ModHelper.Console.WriteLine($"Failed to delete cache\n{exception}", MessageType.Warning);
                return;
            }

            counter++;
        }
    }
}
