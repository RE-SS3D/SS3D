using Coimbra;
using JetBrains.Annotations;
using Serilog;
using SS3D.CodeGeneration.Creators;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

namespace SS3D.Data.AssetDatabases
{
    /// <summary>
    /// An AssetDatabase is a ScriptableObject used to hold an Asset list and to create an Enum based on this list.
    /// It is used to find assets using IDs in a very convenient manner throughout the project.
    /// </summary>
    [CreateAssetMenu(menuName = "SS3D/AssetDatabase", fileName = "AssetDatabase", order = 0)]
    public sealed class AssetDatabase : ScriptableObject
    {
        /// <summary>
        /// The path that the enum will be generated to.
        /// </summary>
        public const string DatabaseAssetPath = "/Scripts/SS3D/Data/Generated";

        /// <summary>
        ///  The namespace that will be included on the generated Enum.
        /// </summary>
        public const string DatabaseAssetNamespaceName = "SS3D.Data.Generated";

        /// <summary>
        ///  The name that the generated enum will have;
        /// </summary>
        public string DatabaseName;

        public string DatabaseID;

        /// <summary>
        /// EagerSerialized keeps hard Object refs in <see cref="Assets"/>. AddressablesAsync loads by GUID at runtime.
        /// </summary>
        public AssetDatabaseLoadMode LoadMode = AssetDatabaseLoadMode.EagerSerialized;

        /// <summary>
        /// GUID keys for <see cref="AssetDatabaseLoadMode.AddressablesAsync"/> databases (no hard Object refs).
        /// </summary>
        public List<string> AssetKeys = new();

#if UNITY_EDITOR
        /// <summary>
        /// The asset group that constitutes this AssetDatabase, the system gets every asset from it and adds to an asset list.
        /// </summary>
        public AddressableAssetGroup AssetGroup;
#endif
        
        /// <summary>
        /// All loaded assets that will be included in the built game (EagerSerialized mode only).
        /// </summary>
        public SerializableDictionary<string, Object> Assets;

#if UNITY_EDITOR
        /// <summary>
        /// Loads all the assets from the asset group to the Assets list (or AssetKeys for async mode).
        /// </summary>
        public void LoadAssetsFromAssetGroup()
        {
            if (LoadMode == AssetDatabaseLoadMode.AddressablesAsync)
            {
                AssetKeys = AssetGroup != null
                    ? AssetGroup.entries.Select(entry => entry.guid).ToList()
                    : new List<string>();
                Assets = new SerializableDictionary<string, Object>();
                EditorUtility.SetDirty(this);
                return;
            }

            Assets = new SerializableDictionary<string, Object>();

            foreach (AddressableAssetEntry entry in AssetGroup.entries)
            {
                Assets.TryAdd(entry.guid, entry.MainAsset);
            }

            EditorUtility.SetDirty(this);
        }
#endif

        /// <summary>
        /// Gets an asset based on its ID (index).
        /// </summary>
        /// <param name="id">Uses the ID of the asset cast into a int to get the asset from a list position.</param>
        /// <typeparam name="T">The type of asset to get.</typeparam>
        /// <returns></returns>
        [CanBeNull]
        public T Get<T>([NotNull] string id)
            where T : Object
        {
            if (LoadMode == AssetDatabaseLoadMode.AddressablesAsync)
            {
                if (AssetProvider.TryGetCached<T>(id, out T cached))
                {
                    return cached;
                }

                Log.Error(
                    $"{nameof(AssetDatabase)} Asset of {id} is not found in the Addressables cache for {DatabaseName} (was PreloadAddressableDatabases called?).");
                return null;
            }

            if (!Assets.TryGetValue(id, out Object asset))
            {
                Log.Error($"{nameof(AssetDatabase)} Asset of {id} is not found on the {DatabaseName} database.");
                return null;
            }

            if (typeof(T).IsSubclassOf(typeof(Component)) && asset is GameObject gameObject)
            {
                return gameObject.GetComponent<T>();
            }

            return asset as T;
        }

        public bool TryGet<T>([NotNull] string index, [CanBeNull] out T asset)
            where T : Object
        {
            if (LoadMode == AssetDatabaseLoadMode.AddressablesAsync)
            {
                return AssetProvider.TryGetCached(index, out asset);
            }

            if (!Assets.TryGetValue(index, out Object foundValue))
            {
                asset = null;
                return false;
            }

            if (typeof(T).IsSubclassOf(typeof(Component)) && foundValue is GameObject gameObject)
            {
                asset = gameObject.GetComponent<T>();
                return asset != null;
            }

            asset = foundValue as T;
            return asset != null;
        }

        /// <summary>
        /// Adds abd asset to the asset database. Should be used only for additional content or runtime stuff.
        /// </summary>
        /// <param name="asset"></param>
        /// <typeparam name="TAsset"></typeparam>
        public void Add<TAsset>([NotNull] TAsset asset)
            where TAsset : Object
        {
            Assets.Add(asset.name, asset);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Initializes all asset databases in the project and adds to the databases list.
        /// </summary>
        public static List<AssetDatabase> FindAllAssetDatabases()
        {
            string[] assets = UnityEditor.AssetDatabase.FindAssets($"t:{typeof(AssetDatabase)}");

            List<AssetDatabase> databases = new();

            for (int index = 0; index < assets.Length; index++)
            {
                string database = assets[index];
                string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(database);
                AssetDatabase assetDatabase = UnityEditor.AssetDatabase.LoadAssetAtPath<AssetDatabase>(assetPath);

                databases.Add(assetDatabase);
            }

            return databases;
        }

        /// <summary>
        /// Generates a script with the data of this database for easy access.
        /// </summary>
        public void GenerateDatabaseCode()
        {
            if (AssetDatabaseSettings.SkipCodeGeneration)
            {
                return;
            }

            List<Object> codegenAssets;
            if (LoadMode == AssetDatabaseLoadMode.AddressablesAsync && AssetGroup != null)
            {
                codegenAssets = AssetGroup.entries
                    .Select(entry => entry.MainAsset)
                    .Where(asset => asset != null)
                    .ToList();
            }
            else
            {
                codegenAssets = Assets != null ? Assets.Values.ToList() : new List<Object>();
            }

            DatabaseScriptCreator.CreateAtPath(DatabaseAssetPath, DatabaseName, codegenAssets, DatabaseAssetNamespaceName);
        }

        public bool AddToAddressables([NotNull] Object asset)
        {
            string path = UnityEditor.AssetDatabase.GetAssetPath(asset);

            if (string.IsNullOrEmpty(path))
            {
                Log.Error($"Asset {asset.name} does not have a valid path, cannot add to addressables.");

                return false;
            }

            string guid = UnityEditor.AssetDatabase.AssetPathToGUID(path);

            if (!AssetGroup)
            {
                Log.Error($"Addressable Asset Group {name} not found, cannot add asset {asset.name} to addressables.");

                return false;
            }

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            AddressableAssetEntry entry = settings.FindAssetEntry(guid);

            if (entry != null)
            {
                Log.Warning($"Asset {asset.name} is already in Addressable Group {entry.parentGroup.name}.");

                return false;
            }

            settings.CreateOrMoveEntry(guid, AssetGroup);

            if (LoadMode == AssetDatabaseLoadMode.AddressablesAsync)
            {
                if (AssetKeys == null)
                {
                    AssetKeys = new List<string>();
                }

                if (!AssetKeys.Contains(guid))
                {
                    AssetKeys.Add(guid);
                }
            }
            else
            {
                Add(asset);
            }

            EditorUtility.SetDirty(asset);
            EditorUtility.SetDirty(this);
            
            UnityEditor.AssetDatabase.SaveAssetIfDirty(asset);
            UnityEditor.AssetDatabase.SaveAssetIfDirty(this);

            return true;
        }
#endif
    }
}
