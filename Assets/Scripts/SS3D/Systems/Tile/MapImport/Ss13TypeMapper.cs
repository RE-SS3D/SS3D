using System;
using System.Collections.Generic;

namespace SS3D.Systems.Tile.MapImport
{
    public readonly struct Ss13TypeMatch
    {
        public MapImportKind Kind { get; }

        public string SoName { get; }

        public string MatchedPrefix { get; }

        public Ss13TypeMatch(MapImportKind kind, string soName, string matchedPrefix)
        {
            Kind = kind;
            SoName = soName;
            MatchedPrefix = matchedPrefix;
        }
    }

    /// <summary>
    /// Longest-prefix matcher over SS13 type paths. Unmapped paths are counted for gap reports.
    /// </summary>
    public sealed class Ss13TypeMapper
    {
        private readonly Ss13TypeMapConfig _config;
        private readonly Dictionary<string, int> _unmapped = new Dictionary<string, int>(StringComparer.Ordinal);

        public Ss13TypeMapper(Ss13TypeMapConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public IReadOnlyDictionary<string, int> UnmappedCounts => _unmapped;

        public bool TryMatch(string path, out Ss13TypeMatch match, bool recordUnmapped = true)
        {
            match = default;
            if (string.IsNullOrEmpty(path))
                return false;

            if (IsAlwaysIgnored(path))
                return false;

            // Floor lights are not wall mounts — leave for a later mapping pass.
            if (path.StartsWith("/obj/machinery/light/floor", StringComparison.Ordinal))
            {
                if (recordUnmapped)
                    RecordUnmapped(path);
                return false;
            }

            foreach (Ss13TypeMapEntry entry in _config.Prefixes)
            {
                if (path.StartsWith(entry.Match, StringComparison.Ordinal))
                {
                    string so = string.IsNullOrEmpty(entry.So) ? DefaultSo(entry.Kind) : entry.So;
                    match = new Ss13TypeMatch(entry.Kind, so, entry.Match);
                    return true;
                }
            }

            // Heuristic fallbacks so a thin table still yields a shell + common infrastructure.
            if (path.StartsWith("/turf/closed/wall", StringComparison.Ordinal) ||
                path.StartsWith("/turf/closed/r_wall", StringComparison.Ordinal))
            {
                match = new Ss13TypeMatch(MapImportKind.Wall, _config.DefaultWall, "(default wall)");
                return true;
            }

            if (path.StartsWith("/turf/open/floor", StringComparison.Ordinal) ||
                path.StartsWith("/turf/open/misc", StringComparison.Ordinal))
            {
                match = new Ss13TypeMatch(MapImportKind.Floor, _config.DefaultFloor, "(default floor)");
                return true;
            }

            if (path.StartsWith("/obj/machinery/door", StringComparison.Ordinal))
            {
                match = new Ss13TypeMatch(MapImportKind.Door, _config.DefaultDoor, "(default door)");
                return true;
            }

            if (path.Contains("/window", StringComparison.Ordinal))
            {
                match = new Ss13TypeMatch(MapImportKind.Window, _config.DefaultWindow, "(default window)");
                return true;
            }

            if (path.StartsWith("/obj/structure/cable", StringComparison.Ordinal))
            {
                match = new Ss13TypeMatch(MapImportKind.Cable, "Cables", "(default cable)");
                return true;
            }

            if (path.StartsWith("/obj/structure/disposalpipe", StringComparison.Ordinal))
            {
                match = new Ss13TypeMatch(MapImportKind.Disposal, "DisposalPipes", "(default disposal pipe)");
                return true;
            }

            if (path.StartsWith("/obj/structure/disposaloutlet", StringComparison.Ordinal))
            {
                match = new Ss13TypeMatch(MapImportKind.DisposalTerminal, "DisposalOutlet", "(default disposal outlet)");
                return true;
            }

            if (path.StartsWith("/obj/machinery/disposal", StringComparison.Ordinal))
            {
                match = new Ss13TypeMatch(MapImportKind.DisposalTerminal, "DisposalBin", "(default disposal bin)");
                return true;
            }

            if (path.StartsWith("/obj/machinery/atmospherics/components/unary/vent_scrubber", StringComparison.Ordinal))
            {
                match = new Ss13TypeMatch(MapImportKind.Scrubber, "Scrubber", "(default scrubber)");
                return true;
            }

            if (path.StartsWith("/obj/machinery/atmospherics/components/unary/vent_pump", StringComparison.Ordinal))
            {
                match = new Ss13TypeMatch(MapImportKind.Vent, "Vent", "(default vent)");
                return true;
            }

            if (path.StartsWith("/obj/machinery/atmospherics/pipe", StringComparison.Ordinal))
            {
                match = new Ss13TypeMatch(MapImportKind.Pipe, MapImportPipeResolver.AtmosPipesWaste, "(default pipe)");
                return true;
            }

            if (path.StartsWith("/obj/machinery/power/apc", StringComparison.Ordinal))
            {
                match = new Ss13TypeMatch(MapImportKind.Apc, "APC", "(default apc)");
                return true;
            }

            if (path.StartsWith("/obj/machinery/light/small", StringComparison.Ordinal))
            {
                match = new Ss13TypeMatch(MapImportKind.Light, "LightBulbFixture", "(default light small)");
                return true;
            }

            if (path.StartsWith("/obj/machinery/light", StringComparison.Ordinal))
            {
                match = new Ss13TypeMatch(MapImportKind.Light, "LightTubeFixture", "(default light)");
                return true;
            }

            if (path.StartsWith("/obj/structure/lattice/catwalk", StringComparison.Ordinal))
            {
                match = new Ss13TypeMatch(MapImportKind.Lattice, "Catwalk", "(default catwalk)");
                return true;
            }

            if (path.StartsWith("/obj/structure/lattice", StringComparison.Ordinal))
            {
                match = new Ss13TypeMatch(MapImportKind.Lattice, "Lattice", "(default lattice)");
                return true;
            }

            if (path.StartsWith("/obj/structure/table/wood", StringComparison.Ordinal))
            {
                match = new Ss13TypeMatch(MapImportKind.Table, "TableWood", "(default table wood)");
                return true;
            }

            if (path.StartsWith("/obj/structure/table", StringComparison.Ordinal))
            {
                match = new Ss13TypeMatch(MapImportKind.Table, "TableSteel", "(default table)");
                return true;
            }

            if (path.StartsWith("/obj/machinery/power/smes", StringComparison.Ordinal))
            {
                match = new Ss13TypeMatch(MapImportKind.Smes, "SMES", "(default smes)");
                return true;
            }

            if (recordUnmapped)
                RecordUnmapped(path);
            return false;
        }

        public void ResetUnmapped() => _unmapped.Clear();

        private void RecordUnmapped(string path)
        {
            _unmapped.TryGetValue(path, out int count);
            _unmapped[path] = count + 1;
        }

        private string DefaultSo(MapImportKind kind) => kind switch
        {
            MapImportKind.Floor => _config.DefaultFloor,
            MapImportKind.Wall => _config.DefaultWall,
            MapImportKind.Window => _config.DefaultWindow,
            MapImportKind.Door => _config.DefaultDoor,
            _ => string.Empty,
        };

        private static bool IsAlwaysIgnored(string path)
        {
            return path.StartsWith("/area/", StringComparison.Ordinal) ||
                   path.StartsWith("/turf/open/space", StringComparison.Ordinal) ||
                   path.StartsWith("/turf/template_noop", StringComparison.Ordinal) ||
                   path.StartsWith("/turf/open/openspace", StringComparison.Ordinal);
        }
    }
}
