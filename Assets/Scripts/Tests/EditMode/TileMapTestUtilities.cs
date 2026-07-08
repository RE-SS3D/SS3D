using Coimbra;
using NUnit.Framework;
using SS3D.Data;
using SS3D.Data.AssetDatabases;
using SS3D.Systems.Tile;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EditorTests
{
    internal static class TileMapTestUtilities
    {
        internal const string DatabaseId = "EditModeTileTests";
        internal const string PrefabId = "edit-mode-tile-prefab";

        private static GameObject _tilePrefab;
        private static bool _assetsRegistered;

        internal readonly struct MapContext
        {
            internal MapContext(TileMap map, TileQueryService query, ConstructionService construction)
            {
                Map = map;
                Query = query;
                Construction = construction;
            }

            public TileMap Map { get; }
            public TileQueryService Query { get; }
            public ConstructionService Construction { get; }
        }

        internal static MapContext CreateContext(List<GameObject> instantiated)
        {
            TileMap map = TileMap.Create("EditModeIntegrationMap");
            instantiated.Add(map.gameObject);

            var query = new TileQueryService(map);
            var construction = new ConstructionService(map, query);
            return new MapContext(map, query, construction);
        }

        internal static void EnsureTestAssetsRegistered()
        {
            if (_assetsRegistered && _tilePrefab != null)
                return;

            if (_tilePrefab != null)
                Object.DestroyImmediate(_tilePrefab);

            _tilePrefab = new GameObject("EditModeTilePrefab");
            _tilePrefab.AddComponent<PlacedTileObject>();
            _tilePrefab.SetActive(false);

            var database = ScriptableObject.CreateInstance<AssetDatabase>();
            database.DatabaseID = DatabaseId;
            database.Assets = new SerializableDictionary<string, Object>();
            database.Assets.TryAdd(PrefabId, _tilePrefab);

            FieldInfo databasesField = typeof(Assets).GetField("Databases", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(databasesField, "Assets.Databases field not found");
            var databases = (Dictionary<string, AssetDatabase>)databasesField.GetValue(null)!;
            databases[DatabaseId] = database;

            _assetsRegistered = true;
        }

        internal static TileObjectSo CreateTileSo(TileLayer layer, string prefabName = null)
        {
            EnsureTestAssetsRegistered();

            ObjectAssetReference prefabRef = ScriptableObject.CreateInstance<ObjectAssetReference>();
            prefabRef.Database = DatabaseId;
            prefabRef.Id = PrefabId;
            prefabRef.name = prefabName ?? $"EditModeTest_{layer}";

            TileObjectSo so = ScriptableObject.CreateInstance<TileObjectSo>();
            so.PrefabAsset = prefabRef;
            so.width = 1;
            so.height = 1;
            so.layer = layer;
            so.genericType = TileObjectGenericType.Floor;
            so.specificType = TileObjectSpecificType.None;
            return so;
        }

        internal static void PlacePlenum(MapContext context, Vector3 position)
        {
            TileObjectSo plenumSo = CreateTileSo(TileLayer.Plenum, "TestPlenum");
            PlaceResult result = context.Construction.TryPlaceTile(plenumSo, position, Direction.North, replaceExisting: false);
            Assert.IsTrue(result.Success, "Expected plenum placement to succeed.");
        }

        internal static void PlaceAirtightWall(MapContext context, Vector3 position)
        {
            TileObjectSo wallSo = CreateTileSo(TileLayer.Turf, "TestWall");
            wallSo.genericType = TileObjectGenericType.Wall;
            wallSo.specificType = TileObjectSpecificType.Steel;
            bool placed = context.Map.PlaceTileObject(
                wallSo,
                position,
                Direction.North,
                skipBuildCheck: true,
                replaceExisting: false,
                skipAdjacency: false,
                out GameObject _);
            Assert.IsTrue(placed, "Expected wall placement to succeed.");
        }

        /// <summary>
        /// Interior plenum cells surrounded by an airtight wall ring so chunk vacuum slots
        /// cannot drain the room through open cardinal edges.
        /// Interior occupies (origin+1, origin+1) through (origin+size, origin+size).
        /// </summary>
        internal static void BuildWalledRoom(MapContext context, int originX, int originZ, int interiorSize)
        {
            int outerSize = interiorSize + 2;
            for (int x = 0; x < outerSize; x++)
            {
                for (int z = 0; z < outerSize; z++)
                    PlacePlenum(context, new Vector3(originX + x, 0, originZ + z));
            }

            for (int x = 0; x < outerSize; x++)
            {
                for (int z = 0; z < outerSize; z++)
                {
                    bool isPerimeter = x == 0 || z == 0 || x == outerSize - 1 || z == outerSize - 1;
                    if (!isPerimeter)
                        continue;

                    PlaceAirtightWall(context, new Vector3(originX + x, 0, originZ + z));
                }
            }
        }

        internal static bool IsLayerEmpty(TileMap map, TileLayer layer, Vector3 position)
        {
            ITileLocation location = map.GetOrCreateTileLocation(layer, position);
            return location.IsFullyEmpty();
        }

        internal sealed class RecordingMutationObserver : ITileMutationObserver
        {
            public int PlacedCount { get; private set; }
            public int ClearedCount { get; private set; }
            public TileCoord LastPlacedCoord { get; private set; }
            public TileCoord LastClearedCoord { get; private set; }
            public TileLayer LastClearedLayer { get; private set; }

            public void OnTilePlaced(ITileOccupant occupant, TileCoord coord)
            {
                PlacedCount++;
                LastPlacedCoord = coord;
            }

            public void OnTileCleared(ITileOccupant occupant, TileCoord coord, TileLayer layer)
            {
                ClearedCount++;
                LastClearedCoord = coord;
                LastClearedLayer = layer;
            }

            public void OnChunkCreated(TileChunkRef chunk) { }

            public void OnTileStateChanged(TileCoord coord) { }
        }
    }
}
