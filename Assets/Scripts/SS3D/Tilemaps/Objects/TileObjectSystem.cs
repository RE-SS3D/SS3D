using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Data;
using SS3D.Logging;
using SS3D.Tilemaps.Adjacency;
using SS3D.Tilemaps.Enums;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Random = UnityEngine.Random;

namespace SS3D.Tilemaps.Objects
{
    public class TileObjectSystem : NetworkedSystem
    {
        private TileSystem _tileSystem;
        private TileAdjacencySystem _tileAdjacencySystem;

        public static Dictionary<TileObjectLayer, List<TileObject>> AssetsPerLayer;
        public static Dictionary<TileObjects, TileObject> Assets;

        protected override void OnStart()
        {
            base.OnStart();

            _tileSystem = GameSystems.Get<TileSystem>();
            _tileAdjacencySystem = GameSystems.Get<TileAdjacencySystem>();

            LoadAssets();
            LoadAssetsPerLayer();
        }

        [ContextMenu("Stress test")]
        public async UniTask StressTest()
        {
            const int testSize = 25;
            int changedTiles = 0;

            for (int i = -testSize; i < testSize; i++)
            {
                for (int j = -testSize; j < testSize; j++)
                {
                    Vector3Int position = new(i, 0, j);

                    TryAddTileObject(position, Vector3.zero, TileObjects.Plenum, out _);
                    changedTiles++;

                    int random = Random.Range(0, 3);

                    TileObjects turf = random switch
                    {
                        0 => TileObjects.SteelWall,
                        1 => TileObjects.TileWood,
                        2 => TileObjects.TileWhite,
                        _ => TileObjects.SteelWall
                    };

                    TryAddTileObject(position, Vector3.zero, turf, out _, true);
                    changedTiles++;
                }
            }

            Punpun.Say(this, $"Stress test tilemap: {changedTiles} changed tiles", Logs.ServerOnly);
        }

        private static void LoadAssetsPerLayer()
        {
            int layerCount = Enum.GetNames(typeof(TileObjectLayer)).Length;

            Dictionary<TileObjectLayer, List<TileObject>> assetReferences = new();

            for (int i = 0; i < layerCount; i++)
            {
                TileObjectLayer objectLayer = (TileObjectLayer)i;
                assetReferences[objectLayer] = Assets.Values.Where(pair => pair._objectLayer == objectLayer).ToList();
            }

            AssetsPerLayer = assetReferences;
        }

        private static void LoadAssets()
        {
            List<AssetReference> assets = AssetData.TileObjects.Assets;
            Dictionary<TileObjects, TileObject> dictionary = new();

            for (int index = 0; index < assets.Count; index++)
            {
                AssetReference assetReference = assets[index];
                GameObject asset = assetReference.Asset as GameObject;

                if (asset == null)
                {
                    continue;
                }

                TileObject tileObject = asset.GetComponent<TileObject>();
                tileObject.Id = (TileObjects)index;

                dictionary[tileObject.Id] = tileObject;
            }

            Assets = dictionary;
        }

        [Server]
        public bool TryAddTileObject(Vector3Int position, Vector3 rotation, TileObjects tileObject, out TileObject placedTileObject, bool replace = false)
        {
            TileData? nullableTile = _tileSystem.GetTile(position);
            TileData tileData = default;

            if (nullableTile != null)
            {
                tileData = (TileData)nullableTile;
            }

            TileObject prefab = Assets[tileObject];

            TileObjectLayer objectLayer = prefab._objectLayer;
            Quaternion quaternion = Quaternion.Euler(rotation);

            if (!tileData.IsLayerEmpty(prefab._objectLayer))
            {
                if (!replace)
                {
                    placedTileObject = null;
                    return false;
                }

                tileData.DestroyObjectAt(objectLayer);
            }


            // placedTileObject = Instantiate(prefab, position, quaternion, tile.TransformCache);
            // ServerManager.Spawn(placedTileObject.GameObjectCache);

            // placedTileObject.SetPositionAndRotation(position, quaternion);

            // tile.AddObjectAt(layer, placedTileObject);

            placedTileObject = null;
            return false;
        }
    }
}