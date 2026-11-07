using SS3D.Data.AssetDatabases;
using SS3D.Systems.Tile.FloorVisuals;
using SS3D.Systems.Tile.GasMarkers;
using SS3D.Systems.Tile.SpawnPoints;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SS3D.Systems.Tile.MapEditor
{
    /// <summary>
    /// Stable catalog keys for <see cref="FloorDecalDefinition"/> entries in the object library.
    /// </summary>
    public static class MapEditorFloorDecalCatalog
    {
        public const string Prefix = "floor-decal:";

        public static string Encode(ushort id) => $"{Prefix}{id}";

        public static bool TryDecode(string assetName, out ushort id)
        {
            id = 0;
            if (string.IsNullOrEmpty(assetName) || !assetName.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
                return false;

            return ushort.TryParse(assetName.AsSpan(Prefix.Length), out id);
        }
    }

    /// <summary>One sub-tab's matches in a palette-wide search.</summary>
    public sealed class MapEditorSearchGroup
    {
        public MapEditorSearchGroup(MapEditorSubTab subTab, IReadOnlyList<MapEditorCatalogEntry> entries)
        {
            SubTab = subTab;
            Entries = entries;
        }

        public MapEditorSubTab SubTab { get; }
        public IReadOnlyList<MapEditorCatalogEntry> Entries { get; }

        /// <summary>Where the matches live, as the grid heads them: "Upper › Machines".</summary>
        public string Heading => $"{MapEditorSubTabs.ModeLabel(SubTab.Mode)} › {SubTab.Label}";
    }

    /// <summary>One heading's entries in a sub-tab's grid; <see cref="Heading"/> is null when the grid draws no headings.</summary>
    public sealed class MapEditorHeadingGroup
    {
        public MapEditorHeadingGroup(string heading, IReadOnlyList<MapEditorCatalogEntry> entries)
        {
            Heading = heading;
            Entries = entries;
        }

        public string Heading { get; }
        public IReadOnlyList<MapEditorCatalogEntry> Entries { get; }
    }

    /// <summary>
    /// One palette slot (palette S6): a plain entry, or a variant family drawn as one slot that cycles its variants.
    /// <see cref="Variants"/> holds the base variant first (<see cref="MapEditorCatalogHeuristics.StandardVariant"/>),
    /// then the rest by asset name.
    /// </summary>
    public sealed class MapEditorPaletteSlot
    {
        public MapEditorPaletteSlot(string family, IReadOnlyList<MapEditorCatalogEntry> variants)
        {
            Family = family;
            Variants = variants;
        }

        /// <summary>The family label; null for a plain entry.</summary>
        public string Family { get; }
        public IReadOnlyList<MapEditorCatalogEntry> Variants { get; }
        /// <summary>Whether the slot cycles: a family folder with one member draws as a plain entry.</summary>
        public bool IsFamily => Family != null && Variants.Count > 1;

        public int IndexOf(MapEditorCatalogEntry entry)
        {
            if (entry == null)
                return -1;

            for (int i = 0; i < Variants.Count; i++)
            {
                if (string.Equals(Variants[i].AssetName, entry.AssetName, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return -1;
        }

        /// <summary>The variant <paramref name="step"/> places from <paramref name="from"/>, wrapping both ways.</summary>
        public MapEditorCatalogEntry Step(MapEditorCatalogEntry from, int step)
        {
            int index = Math.Max(0, IndexOf(from));
            int count = Variants.Count;
            return Variants[((index + step) % count + count) % count];
        }
    }

    /// <summary>
    /// Runtime catalog built from <see cref="MapEditorCatalogSo"/> and live tile assets.
    /// </summary>
    public sealed class MapEditorCatalog
    {
        private readonly Dictionary<string, MapEditorCatalogEntry> _byAssetName = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<MapEditorCatalogEntry> _entries = new();

        public IReadOnlyList<MapEditorCatalogEntry> Entries => _entries;

        /// <summary>Leaves <see cref="MapEditorSandboxRules"/> denied entries out of <see cref="Query"/> (sandbox players).</summary>
        public bool HideSandboxDenied { get; set; }

        public void Build(IReadOnlyList<GenericObjectSo> assets, MapEditorCatalogSo catalogSo)
        {
            _byAssetName.Clear();
            _entries.Clear();

            HashSet<string> mapped = new(StringComparer.OrdinalIgnoreCase);

            if (catalogSo != null)
            {
                foreach (MapEditorCatalogEntry entry in catalogSo.Entries)
                {
                    if (string.IsNullOrWhiteSpace(entry.AssetName))
                        continue;

                    _entries.Add(entry);
                    _byAssetName[entry.AssetName] = entry;
                    mapped.Add(entry.AssetName);
                }
            }

            foreach (GenericObjectSo asset in assets)
            {
                if (mapped.Contains(asset.NameString))
                    continue;

                MapEditorCatalogEntry generated = MapEditorCatalogHeuristics.Infer(asset);
                if (generated == null)
                    continue;

                _entries.Add(generated);
                _byAssetName[asset.NameString] = generated;
            }

            AppendFloorDecals(mapped);
            AppendSpawnPoints(mapped);
            AppendGasMarkers();
        }

        private void AppendFloorDecals(HashSet<string> mapped)
        {
            FloorDecalCatalog catalog = FloorDecalCatalog.Get();
            if (catalog?.Definitions == null)
                return;

            foreach (FloorDecalDefinition definition in catalog.Definitions)
            {
                if (definition == null || definition.Id == 0)
                    continue;

                string assetName = MapEditorFloorDecalCatalog.Encode(definition.Id);
                if (mapped.Contains(assetName))
                    continue;

                MapEditorCatalogEntry entry = Virtual(assetName, MapEditorSubcategory.Decals,
                    "decal", "overlay", definition.DisplayName);
                _entries.Add(entry);
                _byAssetName[assetName] = entry;
            }
        }

        private void AppendGasMarkers()
        {
            foreach (GasMarkerKind kind in Enum.GetValues(typeof(GasMarkerKind)))
            {
                MapEditorCatalogEntry entry = Virtual(MapEditorGasMarkerCatalog.Encode(kind), MapEditorSubcategory.StartingAir,
                    "air", "gas", "atmos", MapEditorGasMarkerCatalog.DisplayName(kind));
                _entries.Add(entry);
                _byAssetName[entry.AssetName] = entry;
            }
        }

        private void AppendSpawnPoints(HashSet<string> mapped)
        {
            foreach (string jobName in MapEditorSpawnCatalog.DefaultJobNames)
            {
                string assetName = MapEditorSpawnCatalog.EncodeJob(jobName);
                if (mapped.Contains(assetName))
                    continue;

                MapEditorCatalogEntry entry = Virtual(assetName, MapEditorSubcategory.SpawnPoints, "spawn", "job", jobName);
                _entries.Add(entry);
                _byAssetName[assetName] = entry;
                mapped.Add(assetName);
            }

            foreach (AntagonistSpawnCategory category in MapEditorSpawnCatalog.MarkerAntagonists)
            {
                string assetName = MapEditorSpawnCatalog.EncodeAntagonist(category);
                if (mapped.Contains(assetName))
                    continue;

                MapEditorCatalogEntry entry = Virtual(assetName, MapEditorSubcategory.SpawnPoints,
                    "spawn", "antag", "antagonist", MapEditorSpawnCatalog.FormatAntagonist(category));
                _entries.Add(entry);
                _byAssetName[assetName] = entry;
                mapped.Add(assetName);
            }
        }

        public bool TryGetEntry(string assetName, out MapEditorCatalogEntry entry) =>
            _byAssetName.TryGetValue(assetName, out entry);

        public bool TryGetAsset(string assetName, TileResourceLoader loader, out GenericObjectSo asset)
        {
            asset = loader.GetAsset(assetName);
            return asset != null;
        }

        public IEnumerable<MapEditorCatalogEntry> Query(MapEditorMode mode, MapEditorSubcategory subcategory, string search)
        {
            IEnumerable<MapEditorCatalogEntry> query = _entries.Where(e => e.Mode == mode && e.Subcategory == subcategory
                && !(HideSandboxDenied && MapEditorSandboxRules.IsDeniedAsset(e.AssetName)));

            if (string.IsNullOrWhiteSpace(search))
            {
                // Marker rails other than Spawn Points list in kind order.
                return MapEditorSubTabs.TryGet(subcategory, out MapEditorSubTab row) && row.IsMarker
                    && subcategory != MapEditorSubcategory.SpawnPoints
                    ? query
                    : query.OrderBy(e => e.AssetName);
            }

            string term = search.Trim();
            return query
                .Where(e => MatchesSearch(e, term))
                .OrderBy(e => e.AssetName);
        }

        /// <summary>
        /// Palette-wide search (creative-mode §2): matches in every tab, grouped by sub-tab in
        /// tab and build order. Sub-tabs the player can't see are left out; an empty term finds nothing.
        /// </summary>
        public IReadOnlyList<MapEditorSearchGroup> Search(string search, bool restrictedPalette)
        {
            List<MapEditorSearchGroup> groups = new();
            if (string.IsNullOrWhiteSpace(search))
                return groups;

            foreach (MapEditorMode mode in MapEditorSubTabs.Modes)
            {
                foreach (MapEditorSubTab row in MapEditorSubTabs.InMode(mode))
                {
                    if (restrictedPalette && row.SandboxRestricted)
                        continue;

                    MapEditorCatalogEntry[] matches = Query(mode, row.Id, search).ToArray();
                    if (matches.Length > 0)
                        groups.Add(new MapEditorSearchGroup(row, matches));
                }
            }

            return groups;
        }

        /// <summary>
        /// A sub-tab's grid split under its folder headings (creative-mode §2): only once the sub-tab
        /// holds <see cref="MapEditorSubTab.HeadingThreshold"/> entries and they carry headings. Headings sort by name
        /// with <see cref="MapEditorCatalogHeuristics.GeneralHeading"/> last; entries keep their order. Otherwise one
        /// group with no heading.
        /// </summary>
        public static IReadOnlyList<MapEditorHeadingGroup> GroupByHeading(MapEditorSubTab row, IReadOnlyList<MapEditorCatalogEntry> entries)
        {
            if (row == null || entries.Count < row.HeadingThreshold || entries.All(e => string.IsNullOrEmpty(e.Heading)))
                return new[] { new MapEditorHeadingGroup(null, entries) };

            return entries
                .GroupBy(e => string.IsNullOrEmpty(e.Heading) ? MapEditorCatalogHeuristics.GeneralHeading : e.Heading,
                    StringComparer.OrdinalIgnoreCase)
                .OrderBy(group => string.Equals(group.Key, MapEditorCatalogHeuristics.GeneralHeading, StringComparison.OrdinalIgnoreCase))
                .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
                .Select(group => new MapEditorHeadingGroup(group.Key, group.ToArray()))
                .ToArray();
        }

        /// <summary>
        /// <paramref name="entries"/> as palette slots: each family's members fold into one slot where
        /// its first member stood; other entries keep their order as single slots.
        /// </summary>
        public static IReadOnlyList<MapEditorPaletteSlot> GroupFamilies(IReadOnlyList<MapEditorCatalogEntry> entries)
        {
            List<MapEditorPaletteSlot> slots = new();
            Dictionary<string, List<MapEditorCatalogEntry>> families = new(StringComparer.OrdinalIgnoreCase);
            foreach (MapEditorCatalogEntry entry in entries)
            {
                if (string.IsNullOrEmpty(entry.Family))
                {
                    slots.Add(new MapEditorPaletteSlot(null, new[] { entry }));
                    continue;
                }

                if (families.TryGetValue(entry.Family, out List<MapEditorCatalogEntry> members))
                {
                    members.Add(entry);
                    continue;
                }

                members = new List<MapEditorCatalogEntry> { entry };
                families[entry.Family] = members;
                slots.Add(new MapEditorPaletteSlot(entry.Family, members));
            }

            foreach (List<MapEditorCatalogEntry> members in families.Values)
            {
                // Stable: the base variant leads, the rest keep the incoming (asset name) order.
                List<MapEditorCatalogEntry> ordered = members
                    .OrderBy(e => e.VariantLabel == MapEditorCatalogHeuristics.StandardVariant ? 0 : 1)
                    .ToList();
                members.Clear();
                members.AddRange(ordered);
            }

            return slots;
        }

        /// <summary>
        /// The slot <paramref name="entry"/> draws in: its whole family in its sub-tab (sandbox-hidden members left out),
        /// or a single slot when it has none. Search, V and the slot arrows all cycle this set.
        /// </summary>
        public MapEditorPaletteSlot SlotOf(MapEditorCatalogEntry entry)
        {
            if (entry == null)
                return null;

            if (string.IsNullOrEmpty(entry.Family))
                return new MapEditorPaletteSlot(null, new[] { entry });

            MapEditorCatalogEntry[] members = Query(entry.Mode, entry.Subcategory, null)
                .Where(e => string.Equals(e.Family, entry.Family, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            return members.Length == 0
                ? new MapEditorPaletteSlot(null, new[] { entry })
                : GroupFamilies(members)[0];
        }

        /// <summary>
        /// The slots a search group draws: one per family, holding the whole family even when only some
        /// variants matched, in the order the first match appears.
        /// </summary>
        public IReadOnlyList<MapEditorPaletteSlot> SlotsFor(IReadOnlyList<MapEditorCatalogEntry> matches)
        {
            List<MapEditorPaletteSlot> slots = new();
            HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
            foreach (MapEditorCatalogEntry entry in matches)
            {
                if (!string.IsNullOrEmpty(entry.Family) && !seen.Add(entry.Family))
                    continue;

                slots.Add(SlotOf(entry));
            }

            return slots;
        }

        /// <summary>
        /// A slot's one-entry label: a decal's, spawn marker's or gas marker's display name, otherwise the asset name
        /// as palette words (<see cref="PaletteLabel"/>).
        /// </summary>
        public static string DisplayLabel(MapEditorCatalogEntry entry)
        {
            string assetName = entry.AssetName;
            if (MapEditorFloorDecalCatalog.TryDecode(assetName, out ushort decalId))
                return FloorDecalCatalog.Get()?.TryGet(decalId, out FloorDecalDefinition decal) == true ? decal.DisplayName : assetName;

            if (MapEditorSpawnCatalog.IsSpawnKey(assetName))
                return MapEditorSpawnCatalog.DisplayName(assetName);

            if (MapEditorGasMarkerCatalog.TryDecode(assetName, out GasMarkerKind gasKind))
                return MapEditorGasMarkerCatalog.DisplayName(gasKind);

            return PaletteLabel(assetName);
        }

        /// <summary>
        /// What the cursor badge says while Edit holds <paramref name="entry"/>: "Office Chair (Blue)" for a variant,
        /// the bare family for its base ("Office Chair"), otherwise <see cref="DisplayLabel"/>.
        /// </summary>
        public static string PlacingLabel(MapEditorCatalogEntry entry)
        {
            if (entry == null)
                return string.Empty;

            if (string.IsNullOrEmpty(entry.Family) || string.IsNullOrEmpty(entry.VariantLabel))
                return DisplayLabel(entry);

            return entry.VariantLabel == MapEditorCatalogHeuristics.StandardVariant
                ? entry.Family
                : $"{entry.Family} ({entry.VariantLabel})";
        }

        /// <summary>Whether <paramref name="assetName"/> is a floor decal, spawn marker or gas marker key, not a tile asset.</summary>
        public static bool IsVirtualKey(string assetName) =>
            MapEditorFloorDecalCatalog.TryDecode(assetName, out _)
            || MapEditorSpawnCatalog.IsSpawnKey(assetName)
            || MapEditorGasMarkerCatalog.TryDecode(assetName, out _);

        /// <summary>
        /// The words that find <paramref name="assetName"/> in search: a marker's job, antagonist or kind,
        /// a decal's name, otherwise the asset name. Map Check's Show in palette searches for this.
        /// </summary>
        public static string SearchTermFor(string assetName)
        {
            if (MapEditorSpawnCatalog.TryDecode(assetName, out SpawnPointKind kind, out string jobName,
                    out AntagonistSpawnCategory category))
                return kind == SpawnPointKind.Job ? jobName : MapEditorSpawnCatalog.FormatAntagonist(category);

            if (MapEditorGasMarkerCatalog.TryDecode(assetName, out GasMarkerKind gasKind))
                return MapEditorGasMarkerCatalog.DisplayName(gasKind);

            if (MapEditorFloorDecalCatalog.TryDecode(assetName, out ushort decalId)
                && FloorDecalCatalog.Get()?.TryGet(decalId, out FloorDecalDefinition decal) == true)
                return decal.DisplayName;

            return assetName;
        }

        /// <summary>
        /// Whether the palette draws this sub-tab: sandbox players never see <see cref="MapEditorSubTab.SandboxRestricted"/>
        /// rows, and a sub-tab with nothing in it is not shown. Rooms lists areas, not catalog entries, so it always shows.
        /// </summary>
        public bool IsShown(MapEditorSubTab row, bool restrictedPalette)
        {
            if (row == null || (restrictedPalette && row.SandboxRestricted))
                return false;

            if (row.Id == MapEditorSubcategory.Rooms)
                return true;

            return Query(row.Mode, row.Id, null).Any();
        }

        /// <summary>
        /// An asset name as palette words, so the two-line slot label can wrap it: "AtmosPipesSupply" →
        /// "Atmos Pipes Supply", "IDCard" → "ID Card". Acronyms and trailing digits stay whole ("APC", "AtmosFilterL2").
        /// </summary>
        public static string PaletteLabel(string assetName) =>
            string.IsNullOrEmpty(assetName)
                ? assetName
                : System.Text.RegularExpressions.Regex.Replace(assetName, "(?<=[a-z])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", " ");

        private static MapEditorCatalogEntry Virtual(string assetName, MapEditorSubcategory subTab, params string[] tags) =>
            new()
            {
                AssetName = assetName,
                Mode = MapEditorSubTabs.Get(subTab).Mode,
                Subcategory = subTab,
                SearchTags = tags,
            };

        private static bool MatchesSearch(MapEditorCatalogEntry entry, string term)
        {
            // A family's label finds every variant ("office chair"), as each variant's own name does.
            if (entry.AssetName.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (!string.IsNullOrEmpty(entry.Family) && entry.Family.Contains(term, StringComparison.OrdinalIgnoreCase)))
                return true;

            if (entry.SearchTags == null)
                return false;

            return entry.SearchTags.Any(tag => tag.Contains(term, StringComparison.OrdinalIgnoreCase));
        }
    }
}
