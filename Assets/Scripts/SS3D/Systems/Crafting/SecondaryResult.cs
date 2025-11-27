using SS3D.Data.AssetDatabases;
using System;

namespace SS3D.Systems.Crafting
{
    [Serializable]
    public struct SecondaryResult
    {
        public WorldObjectAssetReference Asset;
        public uint Amount;

        public SecondaryResult(WorldObjectAssetReference asset, uint amount)
        {
            Asset = asset;
            Amount = amount;
        }
    }
}
