using FishNet;
using FishNet.Connection;
using SS3D.Core;
using SS3D.Systems.Entities;
using SS3D.Systems.Networking;
using SS3D.Systems.Screens;
using SS3D.Systems.Tile.MapEditor;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Shared HashGrid AOI helpers for host overlays and atmos visualization (matches tile MeshRenderer gating).
    /// </summary>
    public static class TileAoiVisibility
    {
        /// <summary>HashGrid neighbor radius (cells) matching FishNet's 3×3 NearbyEntries window.</summary>
        public const int HashGridNeighborRadius = 1;

        /// <summary>Extra tile chunks of padding beyond the HashGrid neighbor window for atmos/overlays.</summary>
        public const int OverlayChunkPad = 1;

        public static bool IsMapEditorAuthoring() =>
            SubSystems.TryGet(out MapEditorSubSystem editor) && editor.IsActive;

        /// <summary>
        /// World position used for local AOI (player entity, else player camera).
        /// </summary>
        public static bool TryGetLocalAoiWorldPosition(out Vector3 worldPosition)
        {
            if (SubSystems.TryGet(out EntitySubSystem entities))
            {
                IReadOnlyList<Entity> players = entities.SpawnedPlayers;
                NetworkConnection local = InstanceFinder.ClientManager?.Connection;
                if (local != null && local.IsValid)
                {
                    for (int i = 0; i < players.Count; i++)
                    {
                        Entity entity = players[i];
                        if (entity != null && entity.Owner == local)
                        {
                            worldPosition = entity.transform.position;
                            return true;
                        }
                    }
                }

                if (entities.LastSpawned != null)
                {
                    worldPosition = entities.LastSpawned.transform.position;
                    return true;
                }
            }

            if (SubSystems.TryGet(out CameraSubSystem cameras) && cameras.PlayerCamera != null)
            {
                worldPosition = cameras.PlayerCamera.transform.position;
                return true;
            }

            worldPosition = default;
            return false;
        }

        public static bool TryGetLocalHashGridCell(out Vector2Int cell)
        {
            if (!TryGetLocalAoiWorldPosition(out Vector3 world))
            {
                cell = default;
                return false;
            }

            cell = TileObserverConstants.GetHashGridCell(world);
            return true;
        }

        /// <summary>
        /// Inclusive tile bounds covering HashGrid neighbors plus <see cref="OverlayChunkPad"/> chunks.
        /// </summary>
        public static bool TryGetLocalAoiTileBounds(out int minX, out int minZ, out int maxX, out int maxZ)
        {
            if (!TryGetLocalHashGridCell(out Vector2Int cell))
            {
                minX = minZ = maxX = maxZ = 0;
                return false;
            }

            int half = TileConstants.ChunkSize;
            int padTiles = OverlayChunkPad * TileConstants.ChunkSize;
            int radius = HashGridNeighborRadius;
            minX = (cell.x - radius) * half - padTiles;
            minZ = (cell.y - radius) * half - padTiles;
            maxX = (cell.x + radius + 1) * half - 1 + padTiles;
            maxZ = (cell.y + radius + 1) * half - 1 + padTiles;
            return true;
        }

        /// <summary>
        /// Whether a tile chunk key lies inside the local HashGrid AOI (plus optional chunk pad).
        /// Map Editor authoring always returns true.
        /// </summary>
        public static bool IsChunkInLocalAoi(Vector2Int chunkKey, int chunkPad = 0)
        {
            if (IsMapEditorAuthoring())
                return true;

            if (!TryGetLocalHashGridCell(out Vector2Int cell))
                return true; // no player yet — keep overlays visible rather than blanking the station

            int radius = HashGridNeighborRadius + chunkPad;
            return Mathf.Abs(chunkKey.x - cell.x) <= radius
                && Mathf.Abs(chunkKey.y - cell.y) <= radius;
        }
    }
}
