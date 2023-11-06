namespace SS3D.Data.AssetDatabases
{
    /// <summary>
    /// Interface used to implement methods and fields for WorldObjectAssets.
    /// </summary>
    public interface IWorldObjectAsset
    {
        /// <summary>
        /// The reference to the WorldObjectAssetReference asset, which is a way to identify which asset this is without in a more automated manner.
        /// This field is setup by the AssetData system automatically when its implemented on a class.
        /// </summary>
        public WorldObjectAssetReference Asset { get; set; }
    }
}