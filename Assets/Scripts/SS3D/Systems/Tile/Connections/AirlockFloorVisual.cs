using SS3D.Rendering.URP;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// The floor under an airlock: a track plus two half tiles, each wearing the floor of the cell on its
    /// side, or hidden so the plenum shows. Lives on the AirlockTile prefab that
    /// <see cref="DoorAdjacencyConnector"/> instantiates; the connector decides the materials.
    /// Stays world-aligned so directional textures (wood grain) never turn with the door.
    /// </summary>
    public sealed class AirlockFloorVisual : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Two-submesh tile whose halves lie north (submesh 0) and south (submesh 1).")]
        private MeshRenderer northSouthTile;

        [SerializeField]
        [Tooltip("Two-submesh tile whose halves lie east (submesh 0) and west (submesh 1).")]
        private MeshRenderer eastWestTile;

        [SerializeField]
        [Tooltip("Track running east-west, under a north- or south-facing door.")]
        private GameObject northSouthTrack;

        [SerializeField]
        [Tooltip("Track running north-south, under an east- or west-facing door.")]
        private GameObject eastWestTrack;

        [SerializeField]
        [Tooltip("Shared atlas materials (Palette) a half tile must not copy: their UVs only fit the floor's own mesh.")]
        private Material[] atlasMaterials = new Material[0];

        private readonly MeshRenderer[] _halves = new MeshRenderer[2];

        public IReadOnlyList<Material> AtlasMaterials => atlasMaterials;

        /// <summary>The half renderer for submesh 0 or 1; null until the first <see cref="Apply"/>.</summary>
        public MeshRenderer GetHalf(int submesh) => _halves[submesh];

        /// <summary>
        /// Shows the mesh and track for the door's axis and dresses each half. A null material hides
        /// that half.
        /// </summary>
        public void Apply(bool northSouth, Material submesh0Material, Material submesh1Material)
        {
            transform.rotation = Quaternion.identity;

            if (northSouthTrack != null)
                northSouthTrack.SetActive(northSouth);
            if (eastWestTrack != null)
                eastWestTrack.SetActive(!northSouth);

            MeshRenderer source = northSouth ? northSouthTile : eastWestTile;
            if (northSouthTile != null)
                northSouthTile.gameObject.SetActive(false);
            if (eastWestTile != null)
                eastWestTile.gameObject.SetActive(false);

            if (source == null || !source.TryGetComponent(out MeshFilter sourceFilter))
                return;

            ApplyHalf(0, source, sourceFilter.sharedMesh, submesh0Material);
            ApplyHalf(1, source, sourceFilter.sharedMesh, submesh1Material);
        }

        private void ApplyHalf(int submesh, MeshRenderer source, Mesh sourceMesh, Material material)
        {
            MeshRenderer half = GetOrCreateHalf(submesh);
            half.GetComponent<MeshFilter>().sharedMesh = AirlockFloorSides.GetHalfMesh(sourceMesh, submesh);
            AirlockFloorSides.CopyRendererSettings(source, half);
            half.renderingLayerMask = DecalRenderingLayers.WithWorldDecals(half.renderingLayerMask);
            if (material != null)
                half.sharedMaterial = material;

            half.gameObject.SetActive(material != null);
        }

        private MeshRenderer GetOrCreateHalf(int submesh)
        {
            if (_halves[submesh] != null)
                return _halves[submesh];

            GameObject halfObject = new($"AirlockFloorHalf{submesh}");
            halfObject.layer = gameObject.layer;
            halfObject.transform.SetParent(transform, worldPositionStays: false);
            halfObject.AddComponent<MeshFilter>();
            _halves[submesh] = halfObject.AddComponent<MeshRenderer>();
            return _halves[submesh];
        }
    }
}
