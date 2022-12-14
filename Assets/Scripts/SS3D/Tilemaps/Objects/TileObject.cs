using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Data;
using SS3D.Tilemaps.Adjacency;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;
using static UnityEditor.PlayerSettings;

namespace SS3D.Tilemaps.Objects
{
    public class TileObject : NetworkedSpessBehaviour
    {
        [HideInInspector] public TileObjects Id;
        public TileLayer Layer;
        public Adjacencies Adjacencies;
        public MeshFilter MeshFilter;

        private List<Component> _renderers;

        [SyncVar(OnChange = "HandlePositionChanged")] public Vector3Int SyncPosition;
        [SyncVar(OnChange = "HandleRotationChanged")] public Quaternion SyncRotation;

        public List<Component> Renderers => _renderers;

        protected override void OnAwake()
        {
            base.OnAwake();

            _renderers = GetComponents(typeof(Renderer)).ToList();
            _renderers.AddRange(GetComponentsInChildren(typeof(Renderer)));
        }

        [Server]
        public void SetPositionAndRotation(Vector3Int position, Quaternion rotation)
        {
            SyncPosition = position;
            SyncRotation = rotation;
        }

        public void HandlePositionChanged(Vector3Int oldPosition, Vector3Int newPosition, bool asServer)
        {
            Position = newPosition;
        }

        public void HandleRotationChanged(Quaternion oldRotation, Quaternion newRotation, bool asServer)
        {
            Rotation = newRotation;
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
                .Objects.TryGetValue(TileLayer.Turf, out tile);
            tileAdjacencySystem.GetTileObjectMesh(position + offset, tile);

            offset.x = 0;
            offset.z = -1;
            tileSystem.GetTile(position + offset)
                .Objects.TryGetValue(TileLayer.Turf, out tile);
            tileAdjacencySystem.GetTileObjectMesh(position + offset, tile);

            offset.x = 1;
            offset.z = -1;
            tileSystem.GetTile(position + offset)
                .Objects.TryGetValue(TileLayer.Turf, out tile);
            tileAdjacencySystem.GetTileObjectMesh(position + offset, tile);

            offset.x = -1;
            offset.z = 0;
            tileSystem.GetTile(position + offset)
                .Objects.TryGetValue(TileLayer.Turf, out tile);
            tileAdjacencySystem.GetTileObjectMesh(position + offset, tile);

            offset.x = +1;
            offset.z = 0;
            tileSystem.GetTile(position + offset)
                .Objects.TryGetValue(TileLayer.Turf, out tile);
            tileAdjacencySystem.GetTileObjectMesh(position + offset, tile);

            offset.x = -1;
            offset.z = 1;
            tileSystem.GetTile(position + offset)
                .Objects.TryGetValue(TileLayer.Turf, out tile);
            tileAdjacencySystem.GetTileObjectMesh(position + offset, tile);

            offset.x = 0;
            offset.z = 1;
            tileSystem.GetTile(position + offset)
                .Objects.TryGetValue(TileLayer.Turf, out tile);
            tileAdjacencySystem.GetTileObjectMesh(position + offset, tile);

            offset.x = 1;
            offset.z = 1;
            tileSystem.GetTile(position + offset)
                .Objects.TryGetValue(TileLayer.Turf, out tile);
            tileAdjacencySystem.GetTileObjectMesh(position + offset, tile);
        }
    }
}