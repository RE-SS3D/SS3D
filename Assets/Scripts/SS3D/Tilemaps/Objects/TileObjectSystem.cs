using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Data;
using SS3D.Logging;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Random = UnityEngine.Random;

namespace SS3D.Tilemaps.Objects
{
    public class TileObjectSystem : NetworkedSystem
    {
        private TileSystem _tileSystem;

        public static Dictionary<TileLayer, List<TileObject>> AssetsPerLayer;
        public static Dictionary<TileObjects, TileObject> Assets;

        protected override void OnStart()
        {
            base.OnStart();

            _tileSystem = GameSystems.Get<TileSystem>();
            
            SetupAssets();
            SetupAssetsPerLayer();
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

        private static void SetupAssetsPerLayer()
        {
            int layerCount = Enum.GetNames(typeof(TileLayer)).Length;

            Dictionary<TileLayer, List<TileObject>> assetReferences = new();

            for (int i = 0; i < layerCount; i++)
            {
                TileLayer layer = (TileLayer)i;
                assetReferences[layer] = Assets.Values.Where(pair => pair.Layer == layer).ToList();
            }

            AssetsPerLayer = assetReferences;
        }

        private static void SetupAssets()
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
            Tile tile = _tileSystem.GetTile(position);

            TileObject prefab = Assets[tileObject];

            TileLayer layer = prefab.Layer;
            Quaternion quaternion = Quaternion.Euler(rotation);

            if (!tile.IsLayerEmpty(prefab.Layer))
            {
                if (!replace)
                {
                    placedTileObject = null;
                    return false;
                }

                tile.DestroyObjectAt(layer);
            }


            placedTileObject = Instantiate(prefab, position, quaternion, tile.TransformCache);
            ServerManager.Spawn(placedTileObject.GameObjectCache);

            placedTileObject.SetPositionAndRotation(position, quaternion);

            tile.AddObjectAt(layer, placedTileObject);

            return false;
        }
    }
}