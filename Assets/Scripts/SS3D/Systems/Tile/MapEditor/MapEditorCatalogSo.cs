using System;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile.MapEditor
{
    /// <summary>
    /// Serializable catalog entry mapping an asset to a library slot.
    /// </summary>
    [Serializable]
    public sealed class MapEditorCatalogEntry
    {
        public string AssetName;
        public MapEditorMode Mode;
        public MapEditorSubcategory Subcategory;
        /// <summary>The grid heading within the sub-tab, from the asset's folder (empty: the sub-tab has none).</summary>
        public string Heading = string.Empty;
        /// <summary>The variant family's label, from a <c>…Variants/</c> folder (empty: not a variant). One palette slot per family.</summary>
        public string Family = string.Empty;
        /// <summary>This variant's label within its family ("Blue", "Left", "Standard"); empty when <see cref="Family"/> is.</summary>
        public string VariantLabel = string.Empty;
        public string[] SearchTags = Array.Empty<string>();
    }

    /// <summary>
    /// Version-controlled taxonomy for the map editor object library. Generated: run
    /// <c>SS3D/Map Editor/Regenerate Catalog</c> rather than editing it (an EditMode test checks it matches).
    /// </summary>
    [CreateAssetMenu(fileName = "MapEditorCatalog", menuName = "SS3D/Map Editor Catalog", order = 0)]
    public sealed class MapEditorCatalogSo : ScriptableObject
    {
        public List<MapEditorCatalogEntry> Entries = new();
    }
}
