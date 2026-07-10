using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Tile;
using UnityEngine;

namespace SS3D.Systems.Area
{
    /// <summary>
    /// Draws per-tile area flood-fill overlays in the Scene view during play mode.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AreaDebugGizmoDrawer : Actor
    {
        private const float TileYOffset = 0.22f;
        private const float TileThickness = 0.04f;
        private const float TileInset = 0.9f;

        protected override void OnStart()
        {
            base.OnStart();

#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            Destroy(this);
#endif
        }

        private void OnDrawGizmos()
        {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            return;
#endif
            if (!AreaDevSettings.ShowAreaGizmos || !UnityEngine.Application.isPlaying)
            {
                return;
            }

            if (!SubSystems.TryGet(out TileSubSystem tileSubSystem))
            {
                return;
            }

            TileMap map = tileSubSystem.CurrentMap;
            ITileQueryService query = tileSubSystem.QueryService;
            if (map == null || query == null)
            {
                return;
            }

            SubSystems.TryGet(out AreaSubSystem areaSubSystem);

#if UNITY_EDITOR
            UnityEditor.Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
#endif

            foreach (TileChunk chunk in map.GetAllChunks())
            {
                if (!chunk.HasAnyAreaIds())
                {
                    continue;
                }

                for (int x = 0; x < TileChunk.ChunkSize; x++)
                {
                    for (int y = 0; y < TileChunk.ChunkSize; y++)
                    {
                        Vector3 world = chunk.GetWorldPosition(x, y);
                        TileCoord coord = query.WorldToTile(world, map.MapId);
                        if (!map.TryGetAreaId(coord, out ushort areaId) || areaId == AreaId.None)
                        {
                            continue;
                        }

                        DrawTileOverlay(world, areaId);
                    }
                }
            }

            if (areaSubSystem == null)
            {
                return;
            }

            foreach (AreaRecord record in areaSubSystem.GetAllAreas())
            {
                if (record.Apc == null)
                {
                    continue;
                }

                Vector3 apcPosition = query.TileToWorld(record.Apc.OriginTile) + new Vector3(0f, 0.35f, 0f);
                Color areaColor = AreaDevSettings.GetColorForArea(record.Id.Value);
                Gizmos.color = areaColor;
                Gizmos.DrawWireSphere(apcPosition, 0.35f);

                Vector2Int facingOffset = TileHelper.CoordinateDifferenceInFrontFacingDirection(record.Apc.FacingDirection);
                Vector3 facingEnd = apcPosition + new Vector3(facingOffset.x, 0f, facingOffset.y);
                Gizmos.DrawLine(apcPosition, facingEnd);

#if UNITY_EDITOR
                string label = string.IsNullOrWhiteSpace(record.DisplayName)
                    ? $"Area {record.Id.Value}"
                    : $"{record.DisplayName} ({record.Id.Value})";

                UnityEditor.Handles.color = areaColor;
                UnityEditor.Handles.Label(apcPosition + Vector3.up * 0.5f, label);
#endif
            }
        }

        private static void DrawTileOverlay(Vector3 world, ushort areaId)
        {
            Vector3 center = world + new Vector3(0f, TileYOffset, 0f);
            Vector3 size = new Vector3(TileInset, TileThickness, TileInset);
            Color color = AreaDevSettings.GetColorForArea(areaId);

            Gizmos.color = color;
            Gizmos.DrawCube(center, size);

            Color wireColor = color;
            wireColor.a = 1f;
            Gizmos.color = wireColor;
            Gizmos.DrawWireCube(center, size);
        }
    }
}
