using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Data.AssetDatabases
{
    /// <summary>
    /// An AddressablesDatabase is a ScriptableObject used to hold an Asset list and to create an Enum based on this list.
    /// It is used to find assets using IDs in a very convenient manner throughout the project.
    /// </summary>
    [CreateAssetMenu(menuName = DatabaseMenuHierarchy + "Addressables", fileName = "AddressablesDatabase", order = 0)]
    public sealed partial class AddressablesDatabase : AssetDatabase
    {
        [SerializeField]
        private List<string> _assetGuids = new();

        /// <inheritdoc />
        public override IReadOnlyCollection<string> AssetGuids => _assetGuids;

        /// <inheritdoc />
        /// <remarks>Addressables uses the GUID itself as its native key.</remarks>
        public override string ResolveKey(string guid) => guid;
    }
}