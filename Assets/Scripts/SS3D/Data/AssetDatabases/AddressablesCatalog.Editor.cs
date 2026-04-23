#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityAssetDatabase = UnityEditor.AssetDatabase;

namespace SS3D.Data.AssetDatabases
{
    public sealed partial class AddressablesCatalog
    {
        /// <inheritdoc />
        /// <remarks>Loads each discovered database's GUID list from its Addressable group before returning.</remarks>
        public override IEnumerable<AssetDatabase> FindAllDatabasesInProject()
        {
            string[] guids = UnityAssetDatabase.FindAssets($"t:{typeof(AddressablesDatabase)}");
            List<AddressablesDatabase> databases = new(guids.Length);

            foreach (AddressablesDatabase database in guids.Select(UnityAssetDatabase.GUIDToAssetPath).
                Select(UnityAssetDatabase.LoadAssetAtPath<AddressablesDatabase>).
                Where(database => database))
            {
                database.LoadAssetsFromAssetGroup();
                databases.Add(database);
            }

            return databases;
        }
    }
}
#endif