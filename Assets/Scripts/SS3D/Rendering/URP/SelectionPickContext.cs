using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Rendering.URP
{
    /// <summary>
    /// Per-frame selection pick state consumed by <see cref="SelectionPickRendererFeature"/>.
    /// Selectables register as <see cref="ISelectionPickSource"/> so the pick pass can draw
    /// ID colours via transient MaterialPropertyBlocks without leaving MPBs on world MeshRenderers
    /// (which breaks SRP Batcher / GPU Instancing).
    /// </summary>
    public static class SelectionPickContext
    {
        /// <summary>
        /// Inflate renderer bounds slightly so thin floors / silhouette edges still intersect the
        /// cursor ray without pulling in the rest of the frustum.
        /// </summary>
        public const float DefaultPickBoundsExpand = 0.2f;

        /// <summary>
        /// World XZ cell size for the pick spatial index. Finer than FishNet HashGrid (16 m) so a
        /// cursor-ray DDA yields tens–low hundreds of candidates instead of tens of thousands.
        /// </summary>
        public const float PickCellSize = 4f;

        /// <summary>Max ray length for spatial collect (meters).</summary>
        public const float MaxPickRayDistance = 100f;

        public struct Request
        {
            public Camera SourceCamera;
            public RenderTexture Target;
            public bool DebugView;
            /// <summary>Screen-space mouse position used to build the pick ray (pixels).</summary>
            public Vector2 ScreenPosition;
            public bool HasScreenPosition;
        }

        /// <summary>One mesh draw for the offscreen pick pass.</summary>
        public struct PickDraw
        {
            public Mesh Mesh;
            public Matrix4x4 Matrix;
            public Color32 Color;
            public int SubmeshIndex;
            public bool Transparent;

            /// <summary>When set, use <c>DrawRenderer</c> (skinned) instead of <c>DrawMesh</c>.</summary>
            public Renderer SkinnedRenderer;
        }

        public interface ISelectionPickSource
        {
            void CollectPickDraws(List<PickDraw> buffer);

            /// <summary>World position used to bucket this source in the pick spatial index.</summary>
            Vector3 PickWorldCenter { get; }

            /// <summary>
            /// When true, always considered during cursor-culled collect (mobile / skinned sources
            /// that may leave their registered cell between frames).
            /// </summary>
            bool PreferAlwaysScan { get; }

            /// <summary>
            /// When true, the index re-reads <see cref="PickWorldCenter"/> every pick. Without it a
            /// source stays in the cell it registered in, and goes unpickable once it moves away.
            /// </summary>
            bool CanMove { get; }
        }

        static Request? s_Request;
        static readonly List<ISelectionPickSource> s_Sources = new();
        static readonly Dictionary<Vector2Int, List<ISelectionPickSource>> s_ByCell = new();
        static readonly Dictionary<ISelectionPickSource, Vector2Int> s_SourceCells = new();
        static readonly List<ISelectionPickSource> s_AlwaysScan = new();
        static readonly List<ISelectionPickSource> s_Movable = new();
        static readonly HashSet<ISelectionPickSource> s_CollectScratch = new();
        static readonly List<Vector2Int> s_RayCellsScratch = new();
        static readonly Plane[] s_FrustumPlanes = new Plane[6];
        static bool s_HasFrustum;
        /// <summary>True when a pick request is active and cursor culling should run (fail closed if ray missing).</summary>
        static bool s_CursorCullArmed;
        static bool s_HasPickRay;
        static Ray s_PickRay;
        static float s_PickBoundsExpand = DefaultPickBoundsExpand;

        public static void SetRequest(Request request)
        {
            s_Request = request;
        }

        public static void ClearRequest()
        {
            s_Request = null;
            s_HasFrustum = false;
            s_CursorCullArmed = false;
            s_HasPickRay = false;
        }

        /// <summary>EditMode / tests: drop all registered pick sources.</summary>
        public static void ClearAllSources()
        {
            s_Sources.Clear();
            s_ByCell.Clear();
            s_SourceCells.Clear();
            s_AlwaysScan.Clear();
            s_Movable.Clear();
            s_CollectScratch.Clear();
            s_RayCellsScratch.Clear();
        }

        public static bool TryGetRequest(out Request request)
        {
            if (s_Request.HasValue && s_Request.Value.SourceCamera != null)
            {
                request = s_Request.Value;
                return true;
            }

            request = default;
            return false;
        }

        public static Vector2Int GetPickCell(Vector3 worldPosition)
        {
            return new Vector2Int(
                Mathf.FloorToInt(worldPosition.x / PickCellSize),
                Mathf.FloorToInt(worldPosition.z / PickCellSize));
        }

        public static void RegisterSource(ISelectionPickSource source)
        {
            if (source == null || s_Sources.Contains(source))
                return;

            s_Sources.Add(source);
            Vector2Int cell = GetPickCell(source.PickWorldCenter);
            s_SourceCells[source] = cell;
            AddToCell(cell, source);

            if (source.PreferAlwaysScan && !s_AlwaysScan.Contains(source))
                s_AlwaysScan.Add(source);

            if (source.CanMove)
                s_Movable.Add(source);
        }

        public static void UnregisterSource(ISelectionPickSource source)
        {
            if (source == null)
                return;

            s_Sources.Remove(source);
            s_AlwaysScan.Remove(source);
            s_Movable.Remove(source);
            if (s_SourceCells.TryGetValue(source, out Vector2Int cell))
            {
                s_SourceCells.Remove(source);
                RemoveFromCell(cell, source);
            }
        }

        /// <summary>
        /// True when <paramref name="bounds"/> intersects the current pick-camera frustum
        /// (or when no frustum was refreshed — fail open so EditMode/tests still collect).
        /// </summary>
        public static bool IsInPickFrustum(Bounds bounds)
        {
            return !s_HasFrustum || GeometryUtility.TestPlanesAABB(s_FrustumPlanes, bounds);
        }

        /// <summary>
        /// True when the cursor ray intersects <paramref name="bounds"/> (slightly expanded).
        /// With no active pick request: fail open (EditMode). With a request but no mouse ray: fail closed.
        /// </summary>
        public static bool IsNearPickCursor(Bounds bounds)
        {
            if (!s_CursorCullArmed)
                return true;

            if (!s_HasPickRay)
                return false;

            Bounds inflated = bounds;
            inflated.Expand(s_PickBoundsExpand);
            return inflated.IntersectRay(s_PickRay);
        }

        public static void CollectPickDraws(List<PickDraw> buffer)
        {
            RefreshCullStateFromRequest();
            buffer.Clear();

            if (!s_CursorCullArmed)
            {
                // EditMode / no request: walk all sources (fail open).
                for (int i = 0; i < s_Sources.Count; i++)
                {
                    ISelectionPickSource source = s_Sources[i];
                    if (source == null)
                        continue;

                    source.CollectPickDraws(buffer);
                }

                return;
            }

            // Live pick request without a mouse ray: fail closed (no draws, no source walk).
            if (!s_HasPickRay)
                return;

            CollectFromSpatialIndex(buffer);
        }

        static void CollectFromSpatialIndex(List<PickDraw> buffer)
        {
            s_CollectScratch.Clear();

            // Before the cell walk, so a source that moved this frame is found where it is now.
            for (int i = 0; i < s_Movable.Count; i++)
                ReindexIfMoved(s_Movable[i]);

            FillRayCells(s_PickRay, s_RayCellsScratch);

            for (int c = 0; c < s_RayCellsScratch.Count; c++)
            {
                Vector2Int cell = s_RayCellsScratch[c];
                for (int dz = -1; dz <= 1; dz++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        Vector2Int padded = new Vector2Int(cell.x + dx, cell.y + dz);
                        if (!s_ByCell.TryGetValue(padded, out List<ISelectionPickSource> list))
                            continue;

                        for (int i = 0; i < list.Count; i++)
                        {
                            ISelectionPickSource source = list[i];
                            if (source != null)
                                s_CollectScratch.Add(source);
                        }
                    }
                }
            }

            for (int i = 0; i < s_AlwaysScan.Count; i++)
            {
                ISelectionPickSource source = s_AlwaysScan[i];
                if (source == null)
                    continue;

                s_CollectScratch.Add(source);
            }

            foreach (ISelectionPickSource source in s_CollectScratch)
                source.CollectPickDraws(buffer);
        }

        static void ReindexIfMoved(ISelectionPickSource source)
        {
            if (source == null)
                return;

            Vector2Int newCell = GetPickCell(source.PickWorldCenter);
            if (!s_SourceCells.TryGetValue(source, out Vector2Int oldCell) || oldCell == newCell)
                return;

            RemoveFromCell(oldCell, source);
            s_SourceCells[source] = newCell;
            AddToCell(newCell, source);
        }

        /// <summary>
        /// 2D Amanatides–Woo style march of XZ pick cells along <paramref name="ray"/>.
        /// </summary>
        static void FillRayCells(Ray ray, List<Vector2Int> cells)
        {
            cells.Clear();

            float near = 0f;
            float far = MaxPickRayDistance;
            if (TryGetRequest(out Request request) && request.SourceCamera != null)
            {
                near = Mathf.Max(0f, request.SourceCamera.nearClipPlane);
                far = Mathf.Min(MaxPickRayDistance, request.SourceCamera.farClipPlane);
            }

            Vector3 start = ray.origin + ray.direction * near;
            Vector3 end = ray.origin + ray.direction * far;
            Vector2Int startCell = GetPickCell(start);
            Vector2Int endCell = GetPickCell(end);

            cells.Add(startCell);
            if (startCell == endCell)
                return;

            float dx = end.x - start.x;
            float dz = end.z - start.z;
            int stepX = dx > 0f ? 1 : (dx < 0f ? -1 : 0);
            int stepZ = dz > 0f ? 1 : (dz < 0f ? -1 : 0);

            float tDeltaX = stepX != 0 ? Mathf.Abs(PickCellSize / dx) : float.PositiveInfinity;
            float tDeltaZ = stepZ != 0 ? Mathf.Abs(PickCellSize / dz) : float.PositiveInfinity;

            float nextBoundaryX = (stepX > 0 ? startCell.x + 1 : startCell.x) * PickCellSize;
            float nextBoundaryZ = (stepZ > 0 ? startCell.y + 1 : startCell.y) * PickCellSize;
            float tMaxX = stepX != 0 ? (nextBoundaryX - start.x) / dx : float.PositiveInfinity;
            float tMaxZ = stepZ != 0 ? (nextBoundaryZ - start.z) / dz : float.PositiveInfinity;

            // Normalize tMax into [0,1] parameter space along start→end (dx/dz already span that).
            // tDelta/tMax above are already in that parameter space when dx = end-start.

            Vector2Int cell = startCell;
            const int maxSteps = 512;
            for (int step = 0; step < maxSteps; step++)
            {
                if (tMaxX < tMaxZ)
                {
                    tMaxX += tDeltaX;
                    cell = new Vector2Int(cell.x + stepX, cell.y);
                }
                else
                {
                    tMaxZ += tDeltaZ;
                    cell = new Vector2Int(cell.x, cell.y + stepZ);
                }

                cells.Add(cell);
                if (cell == endCell)
                    break;
            }
        }

        static void AddToCell(Vector2Int cell, ISelectionPickSource source)
        {
            if (!s_ByCell.TryGetValue(cell, out List<ISelectionPickSource> list))
            {
                list = new List<ISelectionPickSource>(4);
                s_ByCell[cell] = list;
            }

            if (!list.Contains(source))
                list.Add(source);
        }

        static void RemoveFromCell(Vector2Int cell, ISelectionPickSource source)
        {
            if (!s_ByCell.TryGetValue(cell, out List<ISelectionPickSource> list))
                return;

            list.Remove(source);
            if (list.Count == 0)
                s_ByCell.Remove(cell);
        }

        static void RefreshCullStateFromRequest()
        {
            if (!TryGetRequest(out Request request) || request.SourceCamera == null)
            {
                s_HasFrustum = false;
                s_CursorCullArmed = false;
                s_HasPickRay = false;
                return;
            }

            GeometryUtility.CalculateFrustumPlanes(request.SourceCamera, s_FrustumPlanes);
            s_HasFrustum = true;
            // Armed whenever a live pick request exists — missing mouse must not redraw the frustum.
            s_CursorCullArmed = true;
            s_PickBoundsExpand = DefaultPickBoundsExpand;

            if (!request.HasScreenPosition)
            {
                s_HasPickRay = false;
                return;
            }

            s_PickRay = request.SourceCamera.ScreenPointToRay(request.ScreenPosition);
            s_HasPickRay = true;
        }
    }
}
