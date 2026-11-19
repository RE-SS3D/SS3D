using SS3D.Systems.Tile.GasMarkers;
using SS3D.Systems.Tile.MapEditor;
using SS3D.Systems.Tile.SpawnPoints;
using UnityEngine;

namespace SS3D.Systems.Tile.TileMapCreator
{
    /// <summary>
    /// The pin ghost for map editor markers (spawn points, starting air), which have no prefab of their own: a stem and a
    /// head, coloured by the marker's kind.
    /// </summary>
    internal static class ConstructionMarkerGhost
    {
        public static GameObject Create(Vector3 position, string markerAssetName)
        {
            var root = new GameObject("MarkerHologram");
            root.transform.position = position + Vector3.up * 0.15f;

            GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stem.transform.SetParent(root.transform, false);
            stem.transform.localScale = new Vector3(0.08f, 0.45f, 0.08f);
            stem.transform.localPosition = Vector3.up * 0.45f;
            Object.Destroy(stem.GetComponent<Collider>());

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.transform.SetParent(root.transform, false);
            head.transform.localScale = Vector3.one * 0.28f;
            head.transform.localPosition = Vector3.up * 0.95f;
            Object.Destroy(head.GetComponent<Collider>());

            Color color = ColorOf(markerAssetName);
            Tint(stem, color);
            Tint(head, color);
            return root;
        }

        private static Color ColorOf(string markerAssetName)
        {
            if (MapEditorGasMarkerCatalog.TryDecode(markerAssetName, out GasMarkerKind gasKind))
            {
                Color color = MapEditorGasMarkerCatalog.PinColor(gasKind);
                color.a = 0.7f;
                return color;
            }

            return MapEditorSpawnCatalog.TryDecode(markerAssetName, out SpawnPointKind kind, out _, out _)
                   && kind == SpawnPointKind.Antagonist
                ? new Color(1f, 0.35f, 0.25f, 0.7f)
                : new Color(0.25f, 0.75f, 1f, 0.7f);
        }

        private static void Tint(GameObject go, Color color)
        {
            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer == null)
                return;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            Material material = new(shader);
            material.color = color;
            renderer.sharedMaterial = material;
        }
    }
}
