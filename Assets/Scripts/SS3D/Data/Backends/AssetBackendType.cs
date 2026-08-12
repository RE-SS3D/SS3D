namespace SS3D.Data
{
    /// <summary>
    /// Identifies which asset loading backend to use.
    /// Used internally by <see cref="AssetDatabases.AssetCatalog"/> to route asset resolution to the correct backend.
    /// </summary>
    public enum AssetBackendType
    {
        Addressables,
        Resources,
        File
    }
}
