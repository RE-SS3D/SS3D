using System;
using SS3D.Systems.Tile;
using UnityEngine;

namespace SS3D.Systems.Vision
{
    /// <summary>
    /// Polar shadowcasting on the integer tile grid.
    /// </summary>
    public static class VisionGridCaster
    {
        public static void Cast(
            VisionOcclusionProvider occlusion,
            ITileQueryService query,
            Vector3 originWorld,
            float yawRadians,
            float viewRange,
            float viewConeWidthDegrees,
            int sampleCount,
            Span<float> depthsOut)
        {
            if (sampleCount <= 0 || depthsOut.Length < sampleCount)
                throw new ArgumentException("depthsOut must hold at least sampleCount entries.");

            float halfCone = viewConeWidthDegrees * 0.5f * Mathf.Deg2Rad;
            float stepAngle = viewConeWidthDegrees * Mathf.Deg2Rad / sampleCount;

            for (int i = 0; i < sampleCount; i++)
            {
                float angle = yawRadians - halfCone + (stepAngle * i);
                depthsOut[i] = CastRay(occlusion, query, originWorld, angle, viewRange) / viewRange;
            }
        }

        public static float CastRay(
            VisionOcclusionProvider occlusion,
            ITileQueryService query,
            Vector3 originWorld,
            float angleRadians,
            float maxRange)
        {
            int mapId = query.WorldToTile(originWorld).MapId;
            float dirX = Mathf.Sin(angleRadians);
            float dirZ = Mathf.Cos(angleRadians);

            float originX = originWorld.x;
            float originZ = originWorld.z;

            // Tiles are centred on integer coordinates, so a tile spans [c - 0.5, c + 0.5].
            int cellX = Mathf.RoundToInt(originX);
            int cellZ = Mathf.RoundToInt(originZ);

            int stepX = dirX > 0f ? 1 : (dirX < 0f ? -1 : 0);
            int stepZ = dirZ > 0f ? 1 : (dirZ < 0f ? -1 : 0);

            // Ray parameter (world distance, since the direction is unit length) to the next cell
            // boundary on each axis, and the increment needed to cross one full cell.
            float tMaxX = stepX != 0 ? ((cellX + stepX * 0.5f) - originX) / dirX : float.PositiveInfinity;
            float tMaxZ = stepZ != 0 ? ((cellZ + stepZ * 0.5f) - originZ) / dirZ : float.PositiveInfinity;
            float tDeltaX = stepX != 0 ? 1f / Mathf.Abs(dirX) : float.PositiveInfinity;
            float tDeltaZ = stepZ != 0 ? 1f / Mathf.Abs(dirZ) : float.PositiveInfinity;

            TileCoord current = new TileCoord(mapId, cellX, cellZ);

            // Each iteration crosses exactly one cardinal cell boundary; bound the walk so a
            // degenerate ray can never loop forever.
            int maxSteps = Mathf.CeilToInt(maxRange * 2f) + 2;
            for (int i = 0; i < maxSteps; i++)
            {
                float t;
                TileCoord next;

                if (tMaxX <= tMaxZ)
                {
                    t = tMaxX;
                    tMaxX += tDeltaX;
                    next = new TileCoord(mapId, current.Grid.x + stepX, current.Grid.y);
                }
                else
                {
                    t = tMaxZ;
                    tMaxZ += tDeltaZ;
                    next = new TileCoord(mapId, current.Grid.x, current.Grid.y + stepZ);
                }

                if (t > maxRange)
                    break;

                if (!TryGetStepDirection(current, next, out Direction stepDirection))
                    break;

                // A wall on the shared edge stops the ray exactly at the boundary we just reached.
                if (occlusion.BlocksEdge(current, stepDirection))
                    return Mathf.Clamp(t, 0f, maxRange);

                current = next;

                // An opaque tile is visible up to its near edge (the boundary at distance t).
                if (occlusion.IsBlocked(current))
                    return Mathf.Clamp(t, 0f, maxRange);
            }

            return maxRange;
        }

        private static bool TryGetStepDirection(TileCoord from, TileCoord to, out Direction direction)
        {
            Vector2Int delta = to.Grid - from.Grid;
            foreach (Direction candidate in TileHelper.AllDirections())
            {
                Vector2Int step = TileHelper.CoordinateDifferenceInFrontFacingDirection(candidate);
                if (step == delta)
                {
                    direction = candidate;
                    return true;
                }
            }

            direction = default;
            return false;
        }
    }
}
