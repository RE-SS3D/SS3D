using System;
using System.Collections.Generic;
using System.Linq;
using SS3D.Systems.Tile.MapImport.Dmm;
using UnityEngine;

namespace SS3D.Systems.Tile.MapImport
{
    /// <summary>
    /// Builds a placement plan (Plenum + structural + infrastructure overlays) from parsed DMM cells.
    /// Does not touch the live tilemap — safe for EditMode tests.
    /// </summary>
    public static class MapImportPlanner
    {
        public const string PlenumSoName = "Plenum";

        // FurnitureBase is single-occupant: SMES > Vent > Scrubber > DisposalBin > DisposalOutlet > Table.
        private const int FurniturePrioritySmes = 5;
        private const int FurniturePriorityVent = 4;
        private const int FurniturePriorityScrubber = 3;
        private const int FurniturePriorityDisposalBin = 2;
        private const int FurniturePriorityDisposalOutlet = 1;
        private const int FurniturePriorityTable = 0;

        public static MapImportPlan Build(
            DmmMap map,
            Ss13TypeMapper mapper,
            MapImportBBox bbox = default,
            int? zFilter = null)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));
            if (mapper == null)
                throw new ArgumentNullException(nameof(mapper));

            mapper.ResetUnmapped();
            MapImportPlan plan = new MapImportPlan();

            int originX = 0;
            int originY = 0;
            if (map.Cells.Count > 0)
            {
                originX = map.Cells.Min(c => c.X);
                originY = map.Cells.Min(c => c.Y);
            }

            // Pass 1: structural kinds for door inference + wall-neighbour checks.
            Dictionary<(int X, int Y), MapImportKind> structuralKinds = new Dictionary<(int, int), MapImportKind>();
            foreach (DmmCell cell in map.Cells)
            {
                if (zFilter.HasValue && cell.Z != zFilter.Value)
                    continue;
                if (!bbox.Contains(cell.X, cell.Y))
                    continue;
                if (TryClassifyStructural(cell, mapper, recordUnmapped: false, out MapImportKind kind, out _, out _, out _))
                    structuralKinds[(cell.X, cell.Y)] = kind;
            }

            mapper.ResetUnmapped();

            foreach (DmmCell cell in map.Cells)
            {
                if (zFilter.HasValue && cell.Z != zFilter.Value)
                {
                    plan.SkippedCells++;
                    continue;
                }

                if (!bbox.Contains(cell.X, cell.Y))
                {
                    plan.SkippedCells++;
                    continue;
                }

                int worldX = cell.X - originX;
                int worldZ = cell.Y - originY;

                if (!TryBuildCell(cell, mapper, structuralKinds, worldX, worldZ,
                        out MapImportKind structuralKind, out List<MapImportPlacement> placements,
                        out OverlayCounts overlays))
                {
                    plan.SkippedCells++;
                    continue;
                }

                MapImportCellPlan cellPlan = new MapImportCellPlan
                {
                    SourceX = cell.X,
                    SourceY = cell.Y,
                    WorldX = worldX,
                    WorldZ = worldZ,
                };
                cellPlan.Placements.AddRange(placements);
                plan.Cells.Add(cellPlan);

                switch (structuralKind)
                {
                    case MapImportKind.Floor: plan.FloorCells++; break;
                    case MapImportKind.Wall: plan.WallCells++; break;
                    case MapImportKind.Window: plan.WindowCells++; break;
                    case MapImportKind.Door: plan.DoorCells++; break;
                }

                plan.CablePlacements += overlays.Cable;
                plan.PipePlacements += overlays.Pipe;
                plan.DisposalPlacements += overlays.Disposal;
                plan.DisposalTerminalPlacements += overlays.DisposalTerminal;
                plan.VentPlacements += overlays.Vent;
                plan.ScrubberPlacements += overlays.Scrubber;
                plan.ApcPlacements += overlays.Apc;
                plan.LightPlacements += overlays.Light;
                plan.LatticePlacements += overlays.Lattice;
                plan.TablePlacements += overlays.Table;
                plan.SmesPlacements += overlays.Smes;
            }

            foreach (KeyValuePair<string, int> pair in mapper.UnmappedCounts)
                plan.UnmappedCounts[pair.Key] = pair.Value;

            return plan;
        }

        private struct OverlayCounts
        {
            public int Cable;
            public int Pipe;
            public int Disposal;
            public int DisposalTerminal;
            public int Vent;
            public int Scrubber;
            public int Apc;
            public int Light;
            public int Lattice;
            public int Table;
            public int Smes;
        }

        private static bool TryBuildCell(
            DmmCell cell,
            Ss13TypeMapper mapper,
            IReadOnlyDictionary<(int X, int Y), MapImportKind> structuralKinds,
            int worldX,
            int worldZ,
            out MapImportKind structuralKind,
            out List<MapImportPlacement> placements,
            out OverlayCounts overlays)
        {
            structuralKind = MapImportKind.Skip;
            placements = new List<MapImportPlacement>();
            overlays = default;

            bool hasStructural = TryClassifyStructural(cell, mapper, recordUnmapped: false, out structuralKind,
                out string structuralSo, out Direction structuralDir, out bool doorDirExplicit);

            Ss13TypeMatch? cable = null;
            Ss13TypeMatch? disposal = null;
            Ss13TypeMatch? apc = null;
            Ss13TypeMatch? light = null;
            Ss13TypeMatch? lattice = null;
            Direction apcTowardWall = MapImportDirection.ByondDefault;
            Direction lightTowardWall = MapImportDirection.ByondDefault;

            Dictionary<string, Ss13TypeMatch> pipesBySo = new Dictionary<string, Ss13TypeMatch>(StringComparer.Ordinal);

            int furniturePriority = -1;
            string furnitureSo = null;
            Direction furnitureDir = Direction.North;
            MapImportKind furnitureKind = MapImportKind.Skip;

            foreach (DmmAtom atom in cell.Atoms)
            {
                if (!mapper.TryMatch(atom.Path, out Ss13TypeMatch match, recordUnmapped: true))
                    continue;

                switch (match.Kind)
                {
                    case MapImportKind.Wall:
                    case MapImportKind.Window:
                    case MapImportKind.Door:
                    case MapImportKind.Floor:
                        break;
                    case MapImportKind.Cable:
                        cable = Prefer(cable, match);
                        break;
                    case MapImportKind.Pipe:
                    {
                        string pipeSo = MapImportPipeResolver.Resolve(atom.Path, atom);
                        var resolved = new Ss13TypeMatch(MapImportKind.Pipe, pipeSo, match.MatchedPrefix);
                        if (!pipesBySo.TryGetValue(pipeSo, out Ss13TypeMatch existing) ||
                            resolved.MatchedPrefix.Length > existing.MatchedPrefix.Length)
                        {
                            pipesBySo[pipeSo] = resolved;
                        }

                        break;
                    }
                    case MapImportKind.Disposal:
                        disposal = Prefer(disposal, match);
                        break;
                    case MapImportKind.Lattice:
                        lattice = Prefer(lattice, match);
                        break;
                    case MapImportKind.Vent:
                        ConsiderFurniture(FurniturePriorityVent, match.SoName, MapImportDirection.Resolve(atom),
                            MapImportKind.Vent, ref furniturePriority, ref furnitureSo, ref furnitureDir, ref furnitureKind);
                        break;
                    case MapImportKind.Scrubber:
                        ConsiderFurniture(FurniturePriorityScrubber, match.SoName, MapImportDirection.Resolve(atom),
                            MapImportKind.Scrubber, ref furniturePriority, ref furnitureSo, ref furnitureDir, ref furnitureKind);
                        break;
                    case MapImportKind.DisposalTerminal:
                    {
                        int priority = string.Equals(match.SoName, "DisposalBin", StringComparison.Ordinal)
                            ? FurniturePriorityDisposalBin
                            : FurniturePriorityDisposalOutlet;
                        ConsiderFurniture(priority, match.SoName, MapImportDirection.Resolve(atom),
                            MapImportKind.DisposalTerminal, ref furniturePriority, ref furnitureSo, ref furnitureDir,
                            ref furnitureKind);
                        break;
                    }
                    case MapImportKind.Smes:
                        ConsiderFurniture(FurniturePrioritySmes, match.SoName, MapImportDirection.Resolve(atom),
                            MapImportKind.Smes, ref furniturePriority, ref furnitureSo, ref furnitureDir, ref furnitureKind);
                        break;
                    case MapImportKind.Table:
                        ConsiderFurniture(FurniturePriorityTable, match.SoName, MapImportDirection.Resolve(atom),
                            MapImportKind.Table, ref furniturePriority, ref furnitureSo, ref furnitureDir, ref furnitureKind);
                        break;
                    case MapImportKind.Apc:
                        if (TakeIfBetter(ref apc, match))
                        {
                            if (!MapImportDirection.TryResolveExplicit(atom, out apcTowardWall))
                                apcTowardWall = MapImportDirection.ByondDefault;
                        }

                        break;
                    case MapImportKind.Light:
                        if (TakeIfBetter(ref light, match))
                        {
                            if (!MapImportDirection.TryResolveExplicit(atom, out lightTowardWall))
                                lightTowardWall = MapImportDirection.ByondDefault;
                        }

                        break;
                }
            }

            // Space + lattice (no turf) is a valid cell; lattice replaces Plenum.
            if (!hasStructural && !lattice.HasValue)
                return false;

            if (hasStructural && structuralKind == MapImportKind.Door && !doorDirExplicit)
                structuralDir = MapImportDirection.InferDoorDirection(cell.X, cell.Y, structuralKinds);

            string plenumSo = lattice.HasValue ? lattice.Value.SoName : PlenumSoName;
            placements.Add(new MapImportPlacement { SoName = plenumSo, Direction = Direction.North });
            if (lattice.HasValue)
                overlays.Lattice = 1;

            if (hasStructural)
                placements.Add(new MapImportPlacement { SoName = structuralSo, Direction = structuralDir });

            if (cable.HasValue)
            {
                placements.Add(new MapImportPlacement { SoName = cable.Value.SoName, Direction = Direction.North });
                overlays.Cable = 1;
            }

            foreach (KeyValuePair<string, Ss13TypeMatch> pipe in pipesBySo)
            {
                placements.Add(new MapImportPlacement { SoName = pipe.Key, Direction = Direction.North });
                overlays.Pipe++;
            }

            if (disposal.HasValue)
            {
                placements.Add(new MapImportPlacement { SoName = disposal.Value.SoName, Direction = Direction.North });
                overlays.Disposal = 1;
            }

            if (furnitureSo != null)
            {
                placements.Add(new MapImportPlacement { SoName = furnitureSo, Direction = furnitureDir });
                switch (furnitureKind)
                {
                    case MapImportKind.Vent:
                        overlays.Vent = 1;
                        break;
                    case MapImportKind.Scrubber:
                        overlays.Scrubber = 1;
                        break;
                    case MapImportKind.DisposalTerminal:
                        overlays.DisposalTerminal = 1;
                        break;
                    case MapImportKind.Smes:
                        overlays.Smes = 1;
                        break;
                    case MapImportKind.Table:
                        overlays.Table = 1;
                        break;
                }
            }

            // SS13 wall mounts live on the floor turf with dir toward the wall; SS3D mounts on the wall
            // tile facing into the room.
            if (apc.HasValue)
            {
                placements.Add(CreateWallMountPlacement(apc.Value.SoName, apcTowardWall, worldX, worldZ));
                overlays.Apc = 1;
            }

            if (light.HasValue)
            {
                placements.Add(CreateWallMountPlacement(light.Value.SoName, lightTowardWall, worldX, worldZ));
                overlays.Light = 1;
            }

            return true;
        }

        private static bool TryClassifyStructural(
            DmmCell cell,
            Ss13TypeMapper mapper,
            bool recordUnmapped,
            out MapImportKind kind,
            out string soName,
            out Direction dir,
            out bool dirExplicit)
        {
            kind = MapImportKind.Skip;
            soName = string.Empty;
            dir = MapImportDirection.ByondDefault;
            dirExplicit = false;

            Ss13TypeMatch? wall = null;
            Ss13TypeMatch? window = null;
            Ss13TypeMatch? door = null;
            Ss13TypeMatch? floor = null;
            Direction doorDir = MapImportDirection.ByondDefault;
            bool doorExplicit = false;

            foreach (DmmAtom atom in cell.Atoms)
            {
                if (!mapper.TryMatch(atom.Path, out Ss13TypeMatch match, recordUnmapped))
                    continue;

                switch (match.Kind)
                {
                    case MapImportKind.Wall:
                        wall = Prefer(wall, match);
                        break;
                    case MapImportKind.Window:
                        window = Prefer(window, match);
                        break;
                    case MapImportKind.Door:
                        if (TakeIfBetter(ref door, match))
                            doorExplicit = MapImportDirection.TryResolveExplicit(atom, out doorDir);
                        break;
                    case MapImportKind.Floor:
                        floor = Prefer(floor, match);
                        break;
                }
            }

            if (wall.HasValue)
            {
                kind = MapImportKind.Wall;
                soName = wall.Value.SoName;
                return true;
            }

            if (window.HasValue)
            {
                kind = MapImportKind.Window;
                soName = window.Value.SoName;
                return true;
            }

            if (door.HasValue)
            {
                kind = MapImportKind.Door;
                soName = door.Value.SoName;
                dir = doorDir;
                dirExplicit = doorExplicit;
                return true;
            }

            if (floor.HasValue)
            {
                kind = MapImportKind.Floor;
                soName = floor.Value.SoName;
                return true;
            }

            return false;
        }

        private static MapImportPlacement CreateWallMountPlacement(
            string soName,
            Direction towardWall,
            int floorWorldX,
            int floorWorldZ)
        {
            Vector2Int step = TileHelper.CoordinateDifferenceInFrontFacingDirection(towardWall);
            return new MapImportPlacement
            {
                SoName = soName,
                Direction = TileHelper.GetOpposite(towardWall),
                HasWorldOverride = true,
                WorldX = floorWorldX + step.x,
                WorldZ = floorWorldZ + step.y,
            };
        }

        private static void ConsiderFurniture(
            int priority,
            string soName,
            Direction dir,
            MapImportKind kind,
            ref int currentPriority,
            ref string currentSo,
            ref Direction currentDir,
            ref MapImportKind currentKind)
        {
            if (priority <= currentPriority)
                return;
            currentPriority = priority;
            currentSo = soName;
            currentDir = dir;
            currentKind = kind;
        }

        private static Ss13TypeMatch? Prefer(Ss13TypeMatch? current, Ss13TypeMatch next)
        {
            if (!current.HasValue)
                return next;
            return next.MatchedPrefix.Length > current.Value.MatchedPrefix.Length ? next : current;
        }

        /// <summary>Updates <paramref name="current"/> when <paramref name="next"/> has a longer match. Returns whether it won.</summary>
        private static bool TakeIfBetter(ref Ss13TypeMatch? current, Ss13TypeMatch next)
        {
            if (!current.HasValue || next.MatchedPrefix.Length > current.Value.MatchedPrefix.Length)
            {
                current = next;
                return true;
            }

            return false;
        }

        /// <summary>Legacy entry point — prefers explicit dir / path / pixel, else BYOND South.</summary>
        public static Direction ResolveDirection(DmmAtom atom) => MapImportDirection.Resolve(atom);

        public static Direction FromByondDir(int byondDir) => MapImportDirection.FromByondDir(byondDir);
    }
}
