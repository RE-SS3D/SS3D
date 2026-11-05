namespace SS3D.Systems.Tile
{
    /// <summary>What a structure sounds and sheds like when hit. Values travel on the wire.</summary>
    public enum StructuralMaterial : byte
    {
        Metal = 0,
        Wood = 1,
        Glass = 2,
    }

    /// <summary>
    /// Per-object exception to <see cref="StructuralMaterials.Resolve(TileObjectGenericType, TileObjectSpecificType, StructuralMaterialOverride)"/>.
    /// </summary>
    public enum StructuralMaterialOverride
    {
        Derived = 0,
        Metal = 1,
        Wood = 2,
        Glass = 3,
    }

    /// <summary>The one place a tile object's impact material is decided. Unknown objects are metal.</summary>
    public static class StructuralMaterials
    {
        public static StructuralMaterial Resolve(TileObjectSo so)
        {
            return so == null
                ? StructuralMaterial.Metal
                : Resolve(so.genericType, so.specificType, so.impactMaterial);
        }

        public static StructuralMaterial Resolve(TileObjectGenericType genericType,
            TileObjectSpecificType specificType, StructuralMaterialOverride materialOverride)
        {
            switch (materialOverride)
            {
                case StructuralMaterialOverride.Metal:
                    return StructuralMaterial.Metal;
                case StructuralMaterialOverride.Wood:
                    return StructuralMaterial.Wood;
                case StructuralMaterialOverride.Glass:
                    return StructuralMaterial.Glass;
            }

            return specificType switch
            {
                TileObjectSpecificType.Wood or TileObjectSpecificType.Poker => StructuralMaterial.Wood,
                TileObjectSpecificType.Glass => StructuralMaterial.Glass,
                _ => StructuralMaterial.Metal,
            };
        }
    }
}
