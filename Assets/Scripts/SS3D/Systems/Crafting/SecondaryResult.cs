using SS3D.Data.AssetDatabases;
using System;

namespace SS3D.Systems.Crafting
{
    [Serializable]
    public struct SecondaryResult
    {
        public ObjectAssetReference Asset;
        public uint Amount;
        
        public SecondaryResult(ObjectAssetReference asset, uint amount)
        {
            Asset = asset;
            Amount = amount;
        }
    }
}