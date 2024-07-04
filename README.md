# OWFasterLoadAssetBundles

A port of https://github.com/DiFFoZ/BepInExFasterLoadAssetBundles for Outer Wilds.

Makes startup loading time faster by **8%**.

## What it does
Before loading asset bundles, they will be decompressed into `Outer Wilds/Cache/AssetBundles`. Decompressing can help with slow loading of asset bundles or high RAM usage.
