using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Data.AssetDatabases
{
    /// <summary>
    /// An AddressablesDatabase is a ScriptableObject used to hold an Asset list and to create an Enum based on this list.
    /// It is used to find assets using IDs in a very convenient manner throughout the project.
    /// </summary>
    [CreateAssetMenu(menuName = "SS3D/AssetDatabase", fileName = "AddressablesDatabase", order = 0)]
    public sealed partial class AddressablesDatabase : ScriptableObject, IAssetDatabase
    {
        /// <summary>
        ///  The name that the generated enum will have;
        /// </summary>
        public string DatabaseName;

        /// <summary>
        /// The GUID of this <see cref="ScriptableObject"/>
        /// </summary>
        public string DatabaseID;

        /// <summary>
        /// All asset GUIDs registered in this database.
        /// </summary>
        [SerializeField]
        public List<string> AssetGuids;

        /// <summary>
        /// Checks if the database has an asset with the given GUID.
        /// </summary>
        public bool Has(string guid) => AssetGuids != null && AssetGuids.Contains(guid);

        /// <summary>
        /// Returns the GUID itself — Addressables uses GUID as its native key.
        /// </summary>
        public string ResolveKey(string guid) => guid;
    }
}