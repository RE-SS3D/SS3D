using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;
using System.Linq;
using System.IO;

using SS3D.Editor.AssetManagement;

public class AssetDataProcessor : AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        List<string> blacklist = System.IO.File.ReadAllLines("Assets/Engine/AssetManagement/blacklist.txt").ToList();

        foreach (string assetPath in deletedAssets)
        {
            AssetScanner.TryRemoveAddressable(assetPath);
        }

        for (int i = 0; i < movedAssets.Length; i++)
        {
            if(blacklist.Any(movedAssets[i].Contains))
            {
                AssetScanner.TryRemoveAddressable(movedAssets[i]);
            }
            else
            {
                AssetScanner.TryAddAddressable(movedAssets[i]);
            }
        }

        foreach (string assetPath in importedAssets)
        {
            if(!movedAssets.Contains(assetPath) && !blacklist.Any(assetPath.Contains)) //a renamed asset is considered both moved and imported; check to make sure we haven't already processed it
            {
                AssetScanner.TryAddAddressable(assetPath);
            }
        }
    }

}
