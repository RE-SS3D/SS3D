namespace SS3D.Data
{
    public static class Items
    {
        /// <summary>
        /// Generic getter, supports all databases and all asset database enums.
        /// </summary>
        /// <param name="databaseId"></param>
        /// <param name="assetId"></param>
        /// <typeparam name="TAsset"></typeparam>
        /// <returns></returns>
        public static TAsset Get<TAsset>(string assetId) where TAsset : UnityEngine.Object
        {
            return Assets.GetDatabase(nameof(Enums.AssetDatabases.Items)).Get<TAsset>(assetId);
        }
    }
}