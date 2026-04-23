using UnityEngine;

namespace SS3D.Data.AssetDatabases
{
    /// <summary>
    /// <see cref="AssetCatalog"/> that routes to the Addressables backend and resolves
    /// GUIDs using identity mapping (GUID is the Addressables key).
    /// </summary>
    [CreateAssetMenu(menuName = "SS3D/Asset Catalog/Addressables Catalog", fileName = "AddressablesCatalog", order = 0)]
    public sealed partial class AddressablesCatalog : AssetCatalog
    {
        /// <inheritdoc />
        public override AssetBackendType BackendType => AssetBackendType.Addressables;

        /// <inheritdoc />
        public override IAssetBackend CreateBackend() => new AddressablesBackend();
    }
}