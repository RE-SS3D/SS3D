using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FishNet;
using FishNet.Connection;
using SS3D.Core;
using SS3D.Logging;
using SS3D.Systems.Area;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Electricity;
using SS3D.Systems.Furniture.Disposal;
using UnityEngine;

namespace SS3D.Systems.Tile.MapImport
{
    public sealed class MapImportApplyResult
    {
        public int PlacedObjects { get; set; }

        public int MissingAssets { get; set; }

        public int CellCount { get; set; }

        public string ReportPath { get; set; }
    }

    /// <summary>
    /// Applies a <see cref="MapImportPlan"/> to the live tilemap (Play Mode / server).
    /// Prefer <see cref="ApplyRoutine"/> for large maps — a full MetaStation place on the main
    /// thread without yields starves FishNet heartbeats and drops the Host local client.
    /// </summary>
    public static class MapImportApplier
    {
        /// <summary>Placements between <c>yield return null</c> so transport / Editor can tick.</summary>
        public const int YieldEveryPlacements = TileMap.BulkLoadYieldEveryPlacements;

        public static MapImportApplyResult Apply(
            MapImportPlan plan,
            TileMap map,
            TileSubSystem tileSystem,
            bool clearMap,
            string unmappedReportPath = null)
        {
            MapImportApplyResult result = null;
            IEnumerator routine = ApplyRoutine(plan, map, tileSystem, clearMap, unmappedReportPath,
                onComplete: r => result = r, onProgress: null, yieldFrames: false);
            while (routine.MoveNext())
            {
            }

            return result;
        }

        public static IEnumerator ApplyRoutine(
            MapImportPlan plan,
            TileMap map,
            TileSubSystem tileSystem,
            bool clearMap,
            string unmappedReportPath = null,
            Action<MapImportApplyResult> onComplete = null,
            Action<string> onProgress = null) =>
            ApplyRoutine(plan, map, tileSystem, clearMap, unmappedReportPath, onComplete, onProgress,
                yieldFrames: true);

        private static IEnumerator ApplyRoutine(
            MapImportPlan plan,
            TileMap map,
            TileSubSystem tileSystem,
            bool clearMap,
            string unmappedReportPath,
            Action<MapImportApplyResult> onComplete,
            Action<string> onProgress,
            bool yieldFrames)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));
            if (map == null)
                throw new ArgumentNullException(nameof(map));
            if (tileSystem == null)
                throw new ArgumentNullException(nameof(tileSystem));

            bool hasElectricity = SubSystems.TryGet(out ElectricitySubSystem electricity);
            bool hasArea = SubSystems.TryGet(out AreaSubSystem area);
            bool hasDisposal = SubSystems.TryGet(out DisposalSubSystem disposal);
            bool deferredAreaFlood = false;
            bool deferredDisposal = false;

            WorldSimSuspension.Scope simsPaused = WorldSimSuspension.Begin("map import");

            if (hasArea)
            {
                area.BeginDeferredAreaFlood();
                deferredAreaFlood = true;
            }

            if (hasDisposal)
            {
                disposal.BeginDeferredNetworkRebuild();
                deferredDisposal = true;
            }

            map.BeginBulkMutation();

            MapImportApplyResult result = new MapImportApplyResult { CellCount = plan.Cells.Count };

            try
            {
                if (clearMap)
                {
                    onProgress?.Invoke("Clearing map…");
                    map.Clear();
                    if (hasElectricity)
                        electricity.ClearRegisteredDevices();
                    if (yieldFrames)
                        yield return null;
                }

                int sinceYield = 0;
                int cellIndex = 0;
                int cellTotal = plan.Cells.Count;

                foreach (MapImportCellPlan cell in plan.Cells)
                {
                    cellIndex++;
                    foreach (MapImportPlacement placement in cell.Placements)
                    {
                        if (tileSystem.GetAsset(placement.SoName) is not TileObjectSo so)
                        {
                            result.MissingAssets++;
                            Log.Warning(typeof(MapImportApplier),
                                "Map import skipping missing tile asset '{asset}'",
                                Logs.Generic,
                                placement.SoName);
                            continue;
                        }

                        float wx = placement.HasWorldOverride ? placement.WorldX : cell.WorldX;
                        float wz = placement.HasWorldOverride ? placement.WorldZ : cell.WorldZ;
                        Vector3 world = new Vector3(wx, 0f, wz);
                        map.PlaceTileObject(so, world, placement.Direction,
                            skipBuildCheck: true, replaceExisting: true, skipAdjacency: true, out _);
                        result.PlacedObjects++;
                        sinceYield++;

                        if (yieldFrames && sinceYield >= TileMap.BulkLoadYieldEveryPlacements)
                        {
                            sinceYield = 0;
                            onProgress?.Invoke($"Placing {cellIndex}/{cellTotal} cells ({result.PlacedObjects} objects)…");
                            yield return null;
                        }
                    }
                }

                onProgress?.Invoke("Refreshing adjacencies…");
                if (yieldFrames)
                    yield return null;
                map.RefreshAllAdjacencies();

                onProgress?.Invoke("Rebuilding observers…");
                if (yieldFrames)
                    yield return null;
                RebuildHostObservers(map);
                map.RefreshAllHostVisibility();
                // Second pass after observers exist so cover-hide composes with AOI show.
                RebuildHostObservers(map);
                map.RefreshAllHostVisibility();

                if (!string.IsNullOrEmpty(unmappedReportPath))
                {
                    WriteUnmappedReport(unmappedReportPath, plan);
                    result.ReportPath = unmappedReportPath;
                }

                onProgress?.Invoke("Finalizing area / disposal…");
                if (yieldFrames)
                    yield return null;
            }
            finally
            {
                map.EndBulkMutation();

                // Flood after all turfs/APCs exist so seeds see complete walls.
                if (deferredAreaFlood)
                    area.EndDeferredAreaFlood();
                // Rebuild disposal topology once after adjacency refresh (avoids mid-place NRE / thrash).
                if (deferredDisposal)
                    disposal.EndDeferredNetworkRebuild();
                simsPaused.Dispose();
            }

            onProgress?.Invoke("Done.");
            onComplete?.Invoke(result);
        }

        private static void RebuildHostObservers(TileMap map)
        {
            if (InstanceFinder.ServerManager != null
                && InstanceFinder.IsClient
                && InstanceFinder.ClientManager != null)
            {
                NetworkConnection conn = InstanceFinder.ClientManager.Connection;
                if (conn != null && conn.IsValid)
                    InstanceFinder.ServerManager.Objects.RebuildObservers(conn, timedOnly: false);
            }
        }

        public static string WriteUnmappedReport(string path, MapImportPlan plan)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("path,count");
            foreach (KeyValuePair<string, int> pair in plan.UnmappedCounts.OrderByDescending(p => p.Value))
                sb.Append(pair.Key).Append(',').Append(pair.Value).AppendLine();
            File.WriteAllText(path, sb.ToString());
            return path;
        }

        public static string FormatSummary(MapImportPlan plan, MapImportApplyResult apply)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Map import: cells=").Append(plan.Cells.Count)
                .Append(" floors=").Append(plan.FloorCells)
                .Append(" walls=").Append(plan.WallCells)
                .Append(" windows=").Append(plan.WindowCells)
                .Append(" doors=").Append(plan.DoorCells)
                .Append(" cables=").Append(plan.CablePlacements)
                .Append(" pipes=").Append(plan.PipePlacements)
                .Append(" disposals=").Append(plan.DisposalPlacements)
                .Append(" disposalTerminals=").Append(plan.DisposalTerminalPlacements)
                .Append(" vents=").Append(plan.VentPlacements)
                .Append(" scrubbers=").Append(plan.ScrubberPlacements)
                .Append(" apcs=").Append(plan.ApcPlacements)
                .Append(" lights=").Append(plan.LightPlacements)
                .Append(" lattices=").Append(plan.LatticePlacements)
                .Append(" tables=").Append(plan.TablePlacements)
                .Append(" smes=").Append(plan.SmesPlacements)
                .Append(" skipped=").Append(plan.SkippedCells)
                .Append(" placed=").Append(apply?.PlacedObjects ?? 0)
                .Append(" missingAssets=").Append(apply?.MissingAssets ?? 0)
                .Append(" unmappedTypes=").Append(plan.UnmappedCounts.Count);
            return sb.ToString();
        }
    }
}
