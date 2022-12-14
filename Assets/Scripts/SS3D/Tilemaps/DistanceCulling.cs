using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Tilemaps.Objects;
using UnityEngine;

namespace SS3D.Tilemaps
{
    public class DistanceCulling : SpessBehaviour
    {
        [SerializeField] private float _distanceThreshold;
        [SerializeField] private int _checkTimeMilliseconds;

        private TileSystem _tileSystem;
        private CancellationTokenSource _cancellationTokenSource;

        protected override void OnStart()
        {
            base.OnStart();

            _tileSystem = GameSystems.Get<TileSystem>();

            #pragma warning disable CS4014
            CheckTileDistanceTask();
            #pragma warning restore CS4014
        }

        private void OnDisable()
        {
            _cancellationTokenSource?.Cancel();
        }

        private async UniTask CheckTileDistanceTask()
        {
            _cancellationTokenSource = new CancellationTokenSource();

            while (true)
            {
                Dictionary<Vector3Int, TileData?> tiles = _tileSystem.Tiles;

                foreach (KeyValuePair<Vector3Int, TileData?> keyValuePair in tiles)
                {
                    if (keyValuePair.Value == null)
                    {
                        continue;
                    }

                    TileData tile = (TileData)keyValuePair.Value;
                    ProcessTileDistance(keyValuePair.Key, tile);
                }

                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    return;
                }

                await UniTask.Delay(_checkTimeMilliseconds, cancellationToken: _cancellationTokenSource.Token);
            }
        }

        private void ProcessTileDistance(Vector3Int position, TileData tileData)
        {
            float distance = Vector3.Distance(Position, position);

            bool reachedThreshold = distance >= _distanceThreshold; 

            foreach (TileObject value in tileData.Objects.Values)
            {
                foreach (Component component in value.Renderers)
                {
                    Renderer meshRenderer = component as Renderer;
                    if (meshRenderer != null)
                    {
                        meshRenderer.enabled = !reachedThreshold;
                    }
                }
            }
        }
    }
}
