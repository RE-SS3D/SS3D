using System.Collections.Generic;
using System.Linq;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Data;
using SS3D.Tilemaps.Adjacency;
using SS3D.Tilemaps.Enums;
using UnityEngine;

namespace SS3D.Tilemaps.Objects
{
    public class TileObject : NetworkedSpessBehaviour
    {
        public TileObjects Id;
        public TileObjectLayer _objectLayer;
        public Adjacencies Adjacencies;
        public MeshFilter MeshFilter;

        public List<Component> Renderers { get; private set; }

        protected override void OnAwake()
        {
            base.OnAwake();

            Renderers = GetComponents(typeof(Renderer)).ToList();
            Renderers.AddRange(GetComponentsInChildren(typeof(Renderer)));
        }

        [ContextMenu("Stress test")]
        public void UpdateAdjacencies()
        {
            // Get system refs
            TileAdjacencySystem tileAdjacencySystem = GameSystems.Get<TileAdjacencySystem>();
            TileSystem tileSystem = GameSystems.Get<TileSystem>();

            // Update this tile
            Vector3Int position = new Vector3Int((int)Position.x, (int)Position.y, (int)Position.z);
            GameSystems.Get<TileAdjacencySystem>().GetTileObjectMesh(position, this);

            // Update all neighbors
            Vector3Int offset = new Vector3Int();
            TileObject tile;

            offset.x = -1;
            offset.z = -1;
            tileSystem.GetTile(position + offset)
                .Objects.TryGetValue(TileObjectLayer.Turf, out tile);
            tileAdjacencySystem.GetTileObjectMesh(position + offset, tile);

            offset.x = 0;
            offset.z = -1;
            tileSystem.GetTile(position + offset)
                .Objects.TryGetValue(TileObjectLayer.Turf, out tile);
            tileAdjacencySystem.GetTileObjectMesh(position + offset, tile);

            offset.x = 1;
            offset.z = -1;
            tileSystem.GetTile(position + offset)
                .Objects.TryGetValue(TileObjectLayer.Turf, out tile);
            tileAdjacencySystem.GetTileObjectMesh(position + offset, tile);

            offset.x = -1;
            offset.z = 0;
            tileSystem.GetTile(position + offset)
                .Objects.TryGetValue(TileObjectLayer.Turf, out tile);
            tileAdjacencySystem.GetTileObjectMesh(position + offset, tile);

            offset.x = +1;
            offset.z = 0;
            tileSystem.GetTile(position + offset)
                .Objects.TryGetValue(TileObjectLayer.Turf, out tile);
            tileAdjacencySystem.GetTileObjectMesh(position + offset, tile);

            offset.x = -1;
            offset.z = 1;
            tileSystem.GetTile(position + offset)
                .Objects.TryGetValue(TileObjectLayer.Turf, out tile);
            tileAdjacencySystem.GetTileObjectMesh(position + offset, tile);

            offset.x = 0;
            offset.z = 1;
            tileSystem.GetTile(position + offset)
                .Objects.TryGetValue(TileObjectLayer.Turf, out tile);
            tileAdjacencySystem.GetTileObjectMesh(position + offset, tile);

            offset.x = 1;
            offset.z = 1;
            tileSystem.GetTile(position + offset)
                .Objects.TryGetValue(TileObjectLayer.Turf, out tile);
            tileAdjacencySystem.GetTileObjectMesh(position + offset, tile);
        }
    }
}