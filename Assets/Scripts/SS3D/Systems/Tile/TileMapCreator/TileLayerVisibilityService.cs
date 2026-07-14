using SS3D.Core;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SS3D.Systems.Tile.TileMapCreator
{
    /// <summary>
    /// Client-only layer visibility for the tilemap build menu. Dims layer groups locally without networking.
    /// </summary>
    public static class TileLayerVisibilityService
    {
        private const float DimmedAlpha = 0.05f;

        private static readonly Dictionary<TileLayerCategory, bool> GroupVisibility = new();
        private static readonly Dictionary<Renderer, Material[]> OriginalMaterials = new();

        private static Material _dimMaterial;
        private static bool _isActive;

        public static bool IsActive => _isActive;

        public static void Activate()
        {
            if (_isActive)
                return;

            _isActive = true;
            EnsureDefaultVisibility();
            ApplyAllGroups();
        }

        public static void Deactivate()
        {
            if (!_isActive)
                return;

            RestoreAll();
            _isActive = false;
        }

        public static bool IsGroupVisible(TileLayerCategory category) =>
            !GroupVisibility.TryGetValue(category, out bool visible) || visible;

        public static void SetGroupVisible(TileLayerCategory category, bool visible)
        {
            GroupVisibility[category] = visible;

            if (!_isActive)
                return;

            ApplyGroup(category);
        }

        public static void ResetGroupDefaults()
        {
            GroupVisibility.Clear();
            EnsureDefaultVisibility();
        }

        public static void TryApplyPlacedTileObject(PlacedTileObject placedObject)
        {
            if (!_isActive || placedObject == null)
                return;

            if (!TileLayerCategoryMapping.TryGetCategoryForLayer(placedObject.Layer, out TileLayerCategory category))
                return;

            ApplyToRenderers(placedObject.gameObject, IsGroupVisible(category));
        }

        public static void TryApplyPlacedItem(PlacedItemObject placedItem)
        {
            if (!_isActive || placedItem == null)
                return;

            ApplyToRenderers(placedItem.gameObject, IsGroupVisible(TileLayerCategory.Items));
        }

        private static void EnsureDefaultVisibility()
        {
            foreach (TileLayerCategory category in TileLayerCategoryMapping.AllCategories)
                GroupVisibility[category] = true;
        }

        private static void ApplyAllGroups()
        {
            foreach (TileLayerCategory category in TileLayerCategoryMapping.AllCategories)
                ApplyGroup(category);
        }

        private static void ApplyGroup(TileLayerCategory category)
        {
            bool visible = IsGroupVisible(category);

            if (TileLayerCategoryMapping.IsItemsCategory(category))
            {
                ApplyItems(visible);
                return;
            }

            TileSubSystem tileSystem = SubSystems.Get<TileSubSystem>();
            TileMap map = tileSystem?.CurrentMap;
            if (map == null)
                return;

            IReadOnlyList<TileLayer> layers = TileLayerCategoryMapping.GetLayers(category);
            foreach (TileChunk chunk in map.GetAllChunks())
            {
                foreach (PlacedTileObject placedObject in chunk.GetAllTilePlacedObjects())
                {
                    if (!ContainsLayer(layers, placedObject.Layer))
                        continue;

                    ApplyToRenderers(placedObject.gameObject, visible);
                }
            }
        }

        private static void ApplyItems(bool visible)
        {
            TileSubSystem tileSystem = SubSystems.Get<TileSubSystem>();
            TileMap map = tileSystem?.CurrentMap;
            if (map == null)
                return;

            foreach (PlacedItemObject placedItem in map.PlacedItems)
                ApplyToRenderers(placedItem.gameObject, visible);
        }

        private static void ApplyToRenderers(GameObject root, bool visible)
        {
            if (root == null)
                return;

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                    continue;

                if (visible)
                    RestoreRenderer(renderer);
                else
                    DimRenderer(renderer);
            }
        }

        private static void DimRenderer(Renderer renderer)
        {
            if (!OriginalMaterials.ContainsKey(renderer))
                OriginalMaterials[renderer] = renderer.sharedMaterials;

            Material dimMaterial = GetDimMaterial();
            Material[] dimmed = new Material[renderer.sharedMaterials.Length];
            for (int i = 0; i < dimmed.Length; i++)
                dimmed[i] = dimMaterial;

            renderer.sharedMaterials = dimmed;
        }

        private static void RestoreRenderer(Renderer renderer)
        {
            if (!OriginalMaterials.TryGetValue(renderer, out Material[] originals))
                return;

            renderer.sharedMaterials = originals;
            OriginalMaterials.Remove(renderer);
        }

        private static void RestoreAll()
        {
            foreach (KeyValuePair<Renderer, Material[]> pair in OriginalMaterials)
            {
                if (pair.Key != null)
                    pair.Key.sharedMaterials = pair.Value;
            }

            OriginalMaterials.Clear();
        }

        private static Material GetDimMaterial()
        {
            if (_dimMaterial != null)
                return _dimMaterial;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Sprites/Default");

            _dimMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave,
            };

            if (shader.name.Contains("Universal"))
            {
                _dimMaterial.SetFloat("_Surface", 1f);
                _dimMaterial.SetFloat("_Blend", 0f);
                _dimMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                _dimMaterial.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                _dimMaterial.SetFloat("_ZWrite", 0f);
                _dimMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                _dimMaterial.renderQueue = (int)RenderQueue.Transparent;
                _dimMaterial.SetColor("_BaseColor", new Color(0.85f, 0.85f, 0.85f, DimmedAlpha));
            }
            else
            {
                _dimMaterial.color = new Color(1f, 1f, 1f, DimmedAlpha);
            }

            return _dimMaterial;
        }

        private static bool ContainsLayer(IReadOnlyList<TileLayer> layers, TileLayer layer)
        {
            for (int i = 0; i < layers.Count; i++)
            {
                if (layers[i] == layer)
                    return true;
            }

            return false;
        }
    }
}
