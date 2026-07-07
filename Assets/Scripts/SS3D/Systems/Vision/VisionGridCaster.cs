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

            int startX = Mathf.RoundToInt(originWorld.x);
            int startZ = Mathf.RoundToInt(originWorld.z);
            int endX = startX + Mathf.RoundToInt(dirX * maxRange);
            int endZ = startZ + Mathf.RoundToInt(dirZ * maxRange);

            TileCoord current = new TileCoord(mapId, startX, startZ);
            float bestDistance = maxRange;

            foreach (TileCoord stepped in GridLine(current, new TileCoord(mapId, endX, endZ)))
            {
                if (stepped.Grid == current.Grid)
                    continue;

                if (!TryGetStepDirection(current, stepped, out Direction stepDirection))
                    continue;

                if (occlusion.BlocksEdge(current, stepDirection))
                {
                    bestDistance = Mathf.Min(bestDistance, DistanceToEdge(originWorld, current, stepDirection));
                    break;
                }

                current = stepped;

                if (occlusion.IsBlocked(current))
                {
                    bestDistance = Mathf.Min(bestDistance, DistanceToTileBoundary(originWorld, current, dirX, dirZ));
                    break;
                }
            }

            return Mathf.Clamp(bestDistance, 0f, maxRange);
        }

        private static float DistanceToEdge(Vector3 origin, TileCoord from, Direction stepDirection)
        {
            Vector2Int delta = TileHelper.CoordinateDifferenceInFrontFacingDirection(stepDirection);
            float edgeX = from.Grid.x + (delta.x > 0 ? 0.5f : delta.x < 0 ? -0.5f : 0f);
            float edgeZ = from.Grid.y + (delta.y > 0 ? 0.5f : delta.y < 0 ? -0.5f : 0f);

            if (delta.x != 0)
                edgeX = from.Grid.x + delta.x * 0.5f;
            if (delta.y != 0)
                edgeZ = from.Grid.y + delta.y * 0.5f;

            return HorizontalDistance(origin, new Vector3(edgeX, 0f, edgeZ));
        }

        private static float DistanceToTileBoundary(Vector3 origin, TileCoord tile, float dirX, float dirZ)
        {
            float centerX = tile.Grid.x;
            float centerZ = tile.Grid.y;
            float dx = centerX - origin.x;
            float dz = centerZ - origin.z;
            float centerDistance = Mathf.Sqrt(dx * dx + dz * dz);

            float absDirX = Mathf.Abs(dirX);
            float absDirZ = Mathf.Abs(dirZ);
            float boundaryDistance = centerDistance;

            if (absDirX > 0.0001f)
            {
                float toVerticalEdge = (Mathf.Sign(dirX) > 0f ? tile.Grid.x - 0.5f - origin.x : origin.x - (tile.Grid.x + 0.5f)) / absDirX;
                boundaryDistance = Mathf.Min(boundaryDistance, toVerticalEdge);
            }

            if (absDirZ > 0.0001f)
            {
                float toHorizontalEdge = (Mathf.Sign(dirZ) > 0f ? tile.Grid.y - 0.5f - origin.z : origin.z - (tile.Grid.y + 0.5f)) / absDirZ;
                boundaryDistance = Mathf.Min(boundaryDistance, toHorizontalEdge);
            }

            return Mathf.Max(0f, boundaryDistance);
        }

        private static float HorizontalDistance(Vector3 from, Vector3 to)
        {
            float dx = to.x - from.x;
            float dz = to.z - from.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
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

        private static System.Collections.Generic.IEnumerable<TileCoord> GridLine(TileCoord from, TileCoord to)
        {
            int x0 = from.Grid.x;
            int y0 = from.Grid.y;
            int x1 = to.Grid.x;
            int y1 = to.Grid.y;

            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                yield return new TileCoord(from.MapId, x0, y0);

                if (x0 == x1 && y0 == y1)
                    break;

                int e2 = err * 2;
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }

                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }
    }
}
