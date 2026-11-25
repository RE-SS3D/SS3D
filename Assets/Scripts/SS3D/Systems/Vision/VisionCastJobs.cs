using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace SS3D.Systems.Vision
{
    /// <summary>
    /// Fills one level ray per angular bin, through the bin's centre, so a point lookup of a bin finds the ray
    /// through its middle (systems/fov.md Cast).
    /// </summary>
    [BurstCompile]
    internal struct VisionRayCommandsJob : IJobParallelFor
    {
        public Vector3 Center;
        public float FirstAngle;
        public float StepAngle;
        public float Range;
        public QueryParameters Query;

        [WriteOnly]
        public NativeArray<RaycastCommand> Commands;

        public void Execute(int index)
        {
            float radians = FirstAngle + (StepAngle * index);
            Vector3 direction = new(Mathf.Sin(radians), 0f, Mathf.Cos(radians));
            Commands[index] = new RaycastCommand(Center, direction, Query, Range);
        }
    }

    /// <summary>
    /// Turns the batch's hits into the polar depth map and the seen-wall tile grid (D1, D4): depth per bin
    /// as a fraction of the range, and every tile a ray stopped in, taken just inside the surface it hit.
    /// </summary>
    [BurstCompile]
    internal struct VisionHitsJob : IJob
    {
        public Vector3 Center;
        public float Range;
        public Vector2Int TileOrigin;
        public int TileSize;

        [ReadOnly]
        public NativeArray<RaycastCommand> Commands;

        [ReadOnly]
        public NativeArray<RaycastHit> Hits;

        public NativeArray<float> Depths;
        public NativeArray<ushort> DepthUpload;
        public NativeArray<Vector3> Points;
        public NativeArray<byte> SeenTiles;

        public void Execute()
        {
            for (int i = 0; i < SeenTiles.Length; i++)
                SeenTiles[i] = 0;

            for (int i = 0; i < Hits.Length; i++)
            {
                RaycastHit hit = Hits[i];
                float depth;
                if (hit.colliderInstanceID == 0)
                {
                    depth = 1f;
                    Points[i] = Center + (Commands[i].direction * Range);
                }
                else
                {
                    depth = Mathf.Clamp01(hit.distance / Range);
                    Points[i] = hit.point;

                    Vector2Int tile = VisionSubSystem.HitTile(hit.point, hit.normal) - TileOrigin;
                    if (tile.x >= 0 && tile.y >= 0 && tile.x < TileSize && tile.y < TileSize)
                        SeenTiles[tile.x + (tile.y * TileSize)] = 255;
                }

                Depths[i] = depth;
                DepthUpload[i] = (ushort)((depth * 65535f) + 0.5f);
            }
        }
    }
}
