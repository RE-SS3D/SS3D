namespace SS3D.Data
{
    /// <summary>
    /// Identifies which asset loading backend to use when acquiring an asset.
    /// Passed by callers to <see cref="AssetSubSystem.AcquireAsync{T}"/> for explicit backend selection.
    /// </summary>
    public enum AssetBackendType
    {
        Addressables,
        Resources,
        File
    }
}
