using System;
using SS3D.Systems.Tile.MapImport.Dmm;

namespace SS3D.Systems.Tile.MapImport
{
    /// <summary>
    /// Resolves SS13 facing from modern <c>/directional/…</c> path suffixes, <c>dir</c> vars,
    /// or pixel offsets. BYOND's default dir is South when nothing is specified.
    /// </summary>
    public static class MapImportDirection
    {
        /// <summary>BYOND atom default when maps omit dir.</summary>
        public const Direction ByondDefault = Direction.South;

        /// <summary>
        /// True when the atom encodes an explicit facing (path suffix, dir var, or dominant pixel offset).
        /// </summary>
        public static bool TryResolveExplicit(DmmAtom atom, out Direction direction)
        {
            direction = ByondDefault;
            if (atom == null)
                return false;

            if (TryFromPath(atom.Path, out direction))
                return true;

            if (atom.TryGetInt("dir", out int byondDir))
            {
                direction = FromByondDir(byondDir);
                return true;
            }

            int px = 0;
            int py = 0;
            bool hasPx = atom.TryGetInt("pixel_x", out px);
            bool hasPy = atom.TryGetInt("pixel_y", out py);
            if (hasPx || hasPy)
            {
                if (Math.Abs(px) >= Math.Abs(py) && px != 0)
                {
                    direction = px > 0 ? Direction.East : Direction.West;
                    return true;
                }

                if (py != 0)
                {
                    direction = py > 0 ? Direction.North : Direction.South;
                    return true;
                }
            }

            return false;
        }

        public static Direction Resolve(DmmAtom atom, Direction fallback = ByondDefault) =>
            TryResolveExplicit(atom, out Direction direction) ? direction : fallback;

        public static bool TryFromPath(string path, out Direction direction)
        {
            direction = ByondDefault;
            if (string.IsNullOrEmpty(path))
                return false;

            // Prefer the last /directional/<cardinal> segment (tgstation mapping helpers).
            const string marker = "/directional/";
            int idx = path.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
                return false;

            string rest = path[(idx + marker.Length)..];
            int slash = rest.IndexOf('/');
            if (slash >= 0)
                rest = rest[..slash];
            int brace = rest.IndexOf('{');
            if (brace >= 0)
                rest = rest[..brace];
            rest = rest.Trim().TrimEnd(',');

            switch (rest.ToLowerInvariant())
            {
                case "north":
                    direction = Direction.North;
                    return true;
                case "south":
                    direction = Direction.South;
                    return true;
                case "east":
                    direction = Direction.East;
                    return true;
                case "west":
                    direction = Direction.West;
                    return true;
                default:
                    return false;
            }
        }

        public static Direction FromByondDir(int byondDir)
        {
            switch (byondDir)
            {
                case 1: return Direction.North;
                case 2: return Direction.South;
                case 4: return Direction.East;
                case 8: return Direction.West;
                case 5: return Direction.NorthEast;
                case 6: return Direction.SouthEast;
                case 9: return Direction.NorthWest;
                case 10: return Direction.SouthWest;
                default: return ByondDefault;
            }
        }

        /// <summary>
        /// When airlocks omit dir, infer facing from walls along the cardinal axes.
        /// Skips neighbouring doors so middle tiles in a 3–5 door airlock run still see the
        /// walls at the ends of the run (e.g. <c>###AAA###</c> → South; vertical stacks → East).
        /// </summary>
        public static Direction InferDoorDirection(
            int sourceX,
            int sourceY,
            System.Collections.Generic.IReadOnlyDictionary<(int X, int Y), MapImportKind> structuralKinds)
        {
            if (structuralKinds == null)
                return ByondDefault;

            const int maxScan = 8;
            bool nsWalls = FindWallAlongAxis(sourceX, sourceY, 0, 1, maxScan, structuralKinds)
                          || FindWallAlongAxis(sourceX, sourceY, 0, -1, maxScan, structuralKinds);
            bool ewWalls = FindWallAlongAxis(sourceX, sourceY, 1, 0, maxScan, structuralKinds)
                          || FindWallAlongAxis(sourceX, sourceY, -1, 0, maxScan, structuralKinds);

            if (nsWalls && !ewWalls)
                return Direction.East;
            if (ewWalls && !nsWalls)
                return Direction.South;

            if (nsWalls && ewWalls)
            {
                // Ambiguous pocket: prefer the axis of an adjacent door run.
                bool doorRunEW = IsDoor(sourceX + 1, sourceY, structuralKinds)
                                 || IsDoor(sourceX - 1, sourceY, structuralKinds);
                bool doorRunNS = IsDoor(sourceX, sourceY + 1, structuralKinds)
                                 || IsDoor(sourceX, sourceY - 1, structuralKinds);
                if (doorRunEW && !doorRunNS)
                    return Direction.South;
                if (doorRunNS && !doorRunEW)
                    return Direction.East;
            }

            return ByondDefault;
        }

        private static bool IsDoor(
            int x,
            int y,
            System.Collections.Generic.IReadOnlyDictionary<(int X, int Y), MapImportKind> structuralKinds) =>
            structuralKinds.TryGetValue((x, y), out MapImportKind kind) && kind == MapImportKind.Door;

        private static bool IsWallOrWindow(
            int x,
            int y,
            System.Collections.Generic.IReadOnlyDictionary<(int X, int Y), MapImportKind> structuralKinds) =>
            structuralKinds.TryGetValue((x, y), out MapImportKind kind)
            && kind is MapImportKind.Wall or MapImportKind.Window;

        /// <summary>Walk <paramref name="dx"/>/<paramref name="dy"/>, skipping doors, until a wall/window or a stop.</summary>
        private static bool FindWallAlongAxis(
            int sourceX,
            int sourceY,
            int dx,
            int dy,
            int maxScan,
            System.Collections.Generic.IReadOnlyDictionary<(int X, int Y), MapImportKind> structuralKinds)
        {
            for (int step = 1; step <= maxScan; step++)
            {
                int x = sourceX + dx * step;
                int y = sourceY + dy * step;
                if (IsWallOrWindow(x, y, structuralKinds))
                    return true;
                if (IsDoor(x, y, structuralKinds))
                    continue;
                return false;
            }

            return false;
        }
    }
}
