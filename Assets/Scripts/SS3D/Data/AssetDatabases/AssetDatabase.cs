using Coimbra;
using JetBrains.Annotations;
using Serilog;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

namespace SS3D.Data.AssetDatabases
{
    /// <summary>
    /// An AssetDatabase is a ScriptableObject used to hold an Asset list and to create an Enum based on this list.
    /// It is used to find assets using IDs in a very convenient manner throughout the project.
    /// </summary>
    [CreateAssetMenu(menuName = "SS3D/AssetDatabase", fileName = "AssetDatabase", order = 0)]
    public sealed partial class AssetDatabase : ScriptableObject, IAssetDatabase
    {
        /// <summary>
        ///  The name that the generated enum will have;
        /// </summary>
        public string DatabaseName;

        public string DatabaseID;

        /// <summary>
        /// All loaded assets that will be included in the built game.
        /// </summary>
        public SerializableDictionary<string, Object> Assets;

        /// <summary>
        /// All asset references for the assets in the database.
        /// </summary>
        [SerializeField]
        internal SerializableDictionary<string, AssetReference> AssetReferences;

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
            if (!Assets.TryGetValue(id, out Object asset))
            {
                Log.Error($"{nameof(AssetDatabase)} Asset of {id} is not found on the {DatabaseName} database.");

                return null;
            }

            if (typeof(T) != typeof(MonoBehaviour) && asset is GameObject gameObject && gameObject.TryGetComponent(out T component))
            {
                return component;
            }

            return asset as T;
        }

        /// <summary>
        /// Gets an asset based on its ID (index).
        /// </summary>
        /// <param name="id">Uses the ID of the asset cast into a int to get the asset from a list position.</param>
        /// <returns>Asset reference of the asset.</returns>
        [CanBeNull]
        public AssetReference GetReference([NotNull] string id)
        {
            if (AssetReferences.TryGetValue(id, out AssetReference assetReference))
            {
                return assetReference;
            }

            Log.Error("{AssetDatabaseName} Asset of {ID} is not found on the {DatabaseName} database.", nameof(AssetDatabase), id, DatabaseName);

            return null;
        }

        /// <summary>
        /// Checks if the database has an asset with the given ID
        /// </summary>
        /// <param name="id">ID to check</param>
        /// <returns>True if the database contains the ID</returns>
        public bool Has([NotNull] string id) => AssetReferences.ContainsKey(id);

        /// <summary>
        /// Returns the GUID itself — Addressables uses GUID as its native key.
        /// </summary>
        public string ResolveKey([NotNull] string guid) => guid;
    }
}