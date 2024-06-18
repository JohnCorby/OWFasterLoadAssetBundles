using HarmonyLib;
using OWFasterLoadAssetBundles.Helpers;
using OWFasterLoadAssetBundles.Managers;
using OWML.Common;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using UnityEngine;

namespace OWFasterLoadAssetBundles;
[HarmonyPatch]
internal static class Patcher
{
    internal static AssetBundleManager AssetBundleManager { get; private set; } = null!;
    internal static MetadataManager MetadataManager { get; private set; } = null!;

    public static void ChainloaderInitialized()
    {
        // BepInEx is ready to load plugins, patching Unity assetbundles
        AsyncHelper.InitUnitySynchronizationContext();

        var dataPath = new DirectoryInfo(Application.dataPath).Parent.FullName;
        var outputFolder = Path.Combine(dataPath, "Cache", "AssetBundles");
        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        AssetBundleManager = new(outputFolder);
        MetadataManager = new MetadataManager(Path.Combine(outputFolder, "metadata.json"));

        Patch();
    }

    private static void Patch()
    {
        var thisType = typeof(Patcher);
        var harmony = OWFasterLoadAssetBundles.Harmony;
        var allBinding = AccessTools.all;

        // file
        var patchMethod = new HarmonyMethod(thisType.GetMethod(nameof(LoadAssetBundleFromFileFast), allBinding));
        var assetBundleType = typeof(AssetBundle);

        string[] loadNames = [nameof(AssetBundle.LoadFromFile), nameof(AssetBundle.LoadFromFileAsync)];
        foreach (var loadName in loadNames)
        {
            harmony.Patch(AccessTools.Method(assetBundleType, loadName, [typeof(string)]),
                prefix: patchMethod);

            harmony.Patch(AccessTools.Method(assetBundleType, loadName, [typeof(string), typeof(uint)]),
               prefix: patchMethod);

            harmony.Patch(AccessTools.Method(assetBundleType, loadName, [typeof(string), typeof(uint), typeof(ulong)]),
               prefix: patchMethod);
        }

        // streams
        harmony.Patch(AccessTools.Method(assetBundleType, "LoadFromStreamInternal"),
            prefix: new(thisType.GetMethod(nameof(LoadAssetBundleFromStreamFast), allBinding)));

        harmony.Patch(AccessTools.Method(assetBundleType, "LoadFromStreamAsyncInternal"),
            prefix: new(thisType.GetMethod(nameof(LoadAssetBundleFromStreamAsyncFast), allBinding)));
    }

    private static void LoadAssetBundleFromFileFast(ref string path)
    {
        // mod trying to load assetbundle at null path, buh
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        try
        {
            using var bundleFileStream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite);

            if (HandleStreamBundle(bundleFileStream, out var newPath))
            {
                path = newPath;
            }
        }
        catch (Exception ex)
        {
            OWFasterLoadAssetBundles.Instance.ModHelper.Console.WriteLine($"Failed to decompress assetbundle\n{ex}", MessageType.Error);
        }
    }

    private static bool LoadAssetBundleFromStreamFast(Stream stream, ref AssetBundle? __result)
    {
        if (HandleStreamBundle(stream, out var path))
        {
            __result = AssetBundle.LoadFromFile_Internal(path, 0, 0);
            return false;
        }
        
        return true;
    }

    private static bool LoadAssetBundleFromStreamAsyncFast(Stream stream, ref AssetBundleCreateRequest? __result)
    {
        if (HandleStreamBundle(stream, out var path))
        {
            __result = AssetBundle.LoadFromFileAsync_Internal(path, 0, 0);
            return false;
        }
        
        return true;
    }

    private static bool HandleStreamBundle(Stream stream, out string? path)
    {
        var previousPosition = stream.Position;

        try
        {
            return AssetBundleManager.TryRecompressAssetBundle(stream, out path);
        }
        catch (Exception ex)
        {
            OWFasterLoadAssetBundles.Instance.ModHelper.Console.WriteLine($"Failed to decompress assetbundle\n{ex}", MessageType.Error);
        }

        stream.Position = previousPosition;
        path = null!;
        return false;
    }
}
