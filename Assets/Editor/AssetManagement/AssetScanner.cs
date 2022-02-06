using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor;

using System;
using System.IO;
using System.Linq;

namespace SS3D.Editor.AssetManagement
{
    public class AssetScanner : EditorWindow
    {
        [MenuItem("RE:SS3D Editor Tools/Asset Scanner")]
        public static void ShowWindow()
        {
            GUIContent gUIContent = new GUIContent();
            gUIContent.text = "Asset Scanner";
            GetWindow(typeof(AssetScanner)).titleContent = gUIContent;

            GetWindow(typeof(AssetScanner)).Show();
        }

        void OnGUI()
        {
            if(GUILayout.Button("Force Rescan"))
            {
                ForceScan();
            }
        }


        void ForceScan()
        {
            ClearAddressables();
            
            DirectoryInfo root = new DirectoryInfo("Assets/");
            FileTreeRecur(root);
        }

        void ClearAddressables()
        {
            AddressableAssetSettings aaSettings = AddressableAssetSettingsDefaultObject.Settings;
            AddressableAssetGroup group = aaSettings.FindGroup("Autoscanned");

            List<AddressableAssetEntry> removeEntries = group.entries.ToList();

            foreach(AddressableAssetEntry entry in removeEntries)
            {
                aaSettings.RemoveAssetEntry(entry.guid);
            }

            aaSettings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryRemoved, removeEntries, true);        
        }

        void FileTreeRecur(DirectoryInfo directory)
        {
            DirectoryInfo[] subDirectories = directory.GetDirectories();
            FileInfo[] files = directory.GetFiles();

            string rootpath = new DirectoryInfo("Assets/").FullName;
            List<string> blacklist = System.IO.File.ReadAllLines("Assets/Engine/AssetManagement/blacklist.txt").ToList();

            foreach(FileInfo file in files)
            {
                string relativePath = "Assets/"+ GetRelativePath(file.FullName, rootpath);
                if(AssetDatabase.AssetPathToGUID(relativePath) != null && !blacklist.Any(relativePath.Contains))
                {
                    TryAddAddressable(relativePath);
                }
            }

            foreach(DirectoryInfo subDirectory in subDirectories)
            {
                if(!blacklist.Any(subDirectory.Name.Contains))
                {
                    FileTreeRecur(subDirectory);
                }
            }
        }

        string GetRelativePath(string filespec, string folder)
        {
            Uri pathUri = new Uri(filespec);
            // Folders must end in a slash
            if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                folder += Path.DirectorySeparatorChar;
            }
            Uri folderUri = new Uri(folder);
            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', '/'));
        }

        public static void TryAddAddressable(string path)
        {
            AddressableAssetSettings aaSettings = AddressableAssetSettingsDefaultObject.Settings;
            if(aaSettings == null) return;

            AddressableAssetGroup group = aaSettings.FindGroup("Autoscanned");

            var entry = aaSettings.FindAssetEntry(AssetDatabase.AssetPathToGUID(path));
            if(entry == null)
            {
                entry = aaSettings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(path), group);
                aaSettings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
            }

            entry.SetAddress(path.Replace("Assets/",""));
        }
        public static void TryRemoveAddressable(string path)
        {
            AddressableAssetSettings aaSettings = AddressableAssetSettingsDefaultObject.Settings;
            if(aaSettings == null) return;

            AddressableAssetGroup group = aaSettings.FindGroup("Autoscanned");

            var entry = aaSettings.FindAssetEntry(AssetDatabase.AssetPathToGUID(path));
            if(entry != null)
            {
                aaSettings.RemoveAssetEntry(AssetDatabase.AssetPathToGUID(path));
                aaSettings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryRemoved, entry, true);
            }
        }
    }
}