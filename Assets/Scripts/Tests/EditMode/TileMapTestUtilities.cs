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
            if (_assetsRegistered)
                return;

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
        }
    }
}
