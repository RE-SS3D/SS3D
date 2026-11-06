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
                _entries.Add(generated);
                _byAssetName[asset.NameString] = generated;
            }

            AppendFloorDecals(mapped);
            AppendSpawnPlacements(mapped);
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

                var entry = new MapEditorCatalogEntry
                {
                    AssetName = assetName,
                    Mode = MapEditorMode.Upper,
                    Subcategory = MapEditorSubcategory.Overlays,
                    SearchTags = new[] { "overlay", "decal", definition.DisplayName },
                };
                _entries.Add(entry);
                _byAssetName[assetName] = entry;
            }
        }

        private void AppendGasMarkers()
        {
            foreach (GasMarkerKind kind in Enum.GetValues(typeof(GasMarkerKind)))
            {
                var entry = new MapEditorCatalogEntry
                {
                    AssetName = MapEditorGasMarkerCatalog.Encode(kind),
                    Mode = MapEditorMode.Scripting,
                    Subcategory = MapEditorSubcategory.Atmospherics,
                    SearchTags = new[] { "gas", "atmos", MapEditorGasMarkerCatalog.DisplayName(kind) },
                };
                _entries.Add(entry);
                _byAssetName[entry.AssetName] = entry;
            }
        }

        private void AppendSpawnPlacements(HashSet<string> mapped)
        {
            foreach (string jobName in MapEditorSpawnCatalog.DefaultJobNames)
            {
                string assetName = MapEditorSpawnCatalog.EncodeJob(jobName);
                if (mapped.Contains(assetName))
                    continue;

                var entry = new MapEditorCatalogEntry
                {
                    AssetName = assetName,
                    Mode = MapEditorMode.Scripting,
                    Subcategory = MapEditorSubcategory.SpawnPlacements,
                    SearchTags = new[] { "spawn", "job", jobName },
                };
                _entries.Add(entry);
                _byAssetName[assetName] = entry;
                mapped.Add(assetName);
            }

            foreach (AntagonistSpawnCategory category in Enum.GetValues(typeof(AntagonistSpawnCategory)))
            {
                string assetName = MapEditorSpawnCatalog.EncodeAntagonist(category);
                if (mapped.Contains(assetName))
                    continue;

                var entry = new MapEditorCatalogEntry
                {
                    AssetName = assetName,
                    Mode = MapEditorMode.Scripting,
                    Subcategory = MapEditorSubcategory.SpawnPlacements,
                    SearchTags = new[]
                    {
                        "spawn", "antag", "antagonist", MapEditorSpawnCatalog.FormatAntagonist(category),
                    },
                };
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

            if (mode == MapEditorMode.Scripting && subcategory != MapEditorSubcategory.SpawnPlacements)
                return query;

            if (string.IsNullOrWhiteSpace(search))
                return query.OrderBy(e => e.AssetName);

            string term = search.Trim();
            return query
                .Where(e => MatchesSearch(e, term))
                .OrderBy(e => e.AssetName);
        }

        public static bool IsScriptingSubcategory(MapEditorSubcategory subcategory) =>
            subcategory is MapEditorSubcategory.Atmospherics
                or MapEditorSubcategory.SpawnPlacements
                or MapEditorSubcategory.RandomSpawners
                or MapEditorSubcategory.Triggers
                or MapEditorSubcategory.Rooms;

        /// <summary>
        /// Scripting rails that still show the stub message.
        /// </summary>
        public static bool IsUnavailableScriptingSubcategory(MapEditorSubcategory subcategory) =>
            subcategory is MapEditorSubcategory.RandomSpawners or MapEditorSubcategory.Triggers;

        public static MapEditorSubcategory[] GetSubcategories(MapEditorMode mode) =>
            mode switch
            {
                // Primary order matches the current design; Turfs/Overlays trail so assets
                // heuristically tagged into them stay reachable even though the redesigned
                // tab row no longer gives them top billing.
                MapEditorMode.Upper => new[]
                {
                    MapEditorSubcategory.Flooring, MapEditorSubcategory.Walls, MapEditorSubcategory.Doors,
                    MapEditorSubcategory.TileObjects, MapEditorSubcategory.WallAttachments,
                    MapEditorSubcategory.Turfs, MapEditorSubcategory.Overlays,
                },
                MapEditorMode.Lower => new[]
                {
                    MapEditorSubcategory.BaseTiles, MapEditorSubcategory.Piping, MapEditorSubcategory.Disposals,
                },
                MapEditorMode.Items => new[]
                {
                    MapEditorSubcategory.FoodDrink, MapEditorSubcategory.Tools, MapEditorSubcategory.Medical,
                    MapEditorSubcategory.Security, MapEditorSubcategory.Misc,
                },
                MapEditorMode.Scripting => new[]
                {
                    MapEditorSubcategory.Atmospherics, MapEditorSubcategory.SpawnPlacements,
                    MapEditorSubcategory.RandomSpawners, MapEditorSubcategory.Triggers, MapEditorSubcategory.Rooms,
                },
                _ => Array.Empty<MapEditorSubcategory>(),
            };

        public static string GetModeLabel(MapEditorMode mode) =>
            mode switch
            {
                MapEditorMode.Upper => "Upper",
                MapEditorMode.Lower => "Lower",
                MapEditorMode.Items => "Items",
                MapEditorMode.Scripting => "Scripts",
                _ => mode.ToString(),
            };

        public static string GetSubcategoryLabel(MapEditorSubcategory subcategory) =>
            subcategory switch
            {
                MapEditorSubcategory.Flooring => "Flooring",
                MapEditorSubcategory.Turfs => "Turfs",
                MapEditorSubcategory.Overlays => "Overlays",
                MapEditorSubcategory.Walls => "Walls",
                MapEditorSubcategory.Doors => "Doors",
                MapEditorSubcategory.TileObjects => "Furniture",
                MapEditorSubcategory.WallAttachments => "Wall Attachments",
                MapEditorSubcategory.Piping => "Piping",
                MapEditorSubcategory.Disposals => "Disposals",
                MapEditorSubcategory.BaseTiles => "Plenum",
                MapEditorSubcategory.FoodDrink => "Food & Drink",
                MapEditorSubcategory.Tools => "Tools",
                MapEditorSubcategory.Medical => "Medical",
                MapEditorSubcategory.Security => "Security",
                MapEditorSubcategory.Misc => "Misc",
                MapEditorSubcategory.Atmospherics => "Atmospherics",
                MapEditorSubcategory.SpawnPlacements => "Spawn Placements",
                MapEditorSubcategory.RandomSpawners => "Random Item Spawners",
                MapEditorSubcategory.Triggers => "Triggers",
                MapEditorSubcategory.Rooms => "Rooms",
                MapEditorSubcategory.Uncategorized => "Uncategorized",
                _ => subcategory.ToString(),
            };

        private static bool MatchesSearch(MapEditorCatalogEntry entry, string term)
        {
            if (entry.AssetName.Contains(term, StringComparison.OrdinalIgnoreCase))
                return true;

            if (entry.SearchTags == null)
                return false;

            return entry.SearchTags.Any(tag => tag.Contains(term, StringComparison.OrdinalIgnoreCase));
        }
    }
}
