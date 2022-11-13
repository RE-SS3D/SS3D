using System;
using System.Collections.Generic;
using Coimbra;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core.Behaviours;
using SS3D.Data;
using SS3D.Tilemaps.Objects;
using UnityEngine;

namespace SS3D.Tilemaps
{
    public class Tile : NetworkedSpessBehaviour
    {
        public event Action<TileLayer, TileObject> OnTileObjectChanged;

        [SyncObject] public readonly SyncDictionary<TileLayer, TileObject> Objects = new();
        [SyncVar(OnChange = "HandleTilePositionChanged")] public Vector3Int TilePosition;

        protected override void OnStart()
        {
            base.OnStart();

            Objects.OnChange += HandleTileObjectsChanged;
        }

        private void HandleTilePositionChanged(Vector3Int oldPosition, Vector3Int newPosition, bool asServer)
        {
            Position = newPosition;
        }

        private void HandleTileObjectsChanged(SyncDictionaryOperation op, TileLayer layer, TileObject tileObject, bool asServer)
        {
            OnTileObjectChanged?.Invoke(layer, tileObject);
        }

        public void AddObjectAt(TileLayer layer, TileObject tileObject)
        {
            Objects.Add(layer, tileObject);
        }

        [Server]
        public void DestroyObjectAt(TileLayer layer)
        {
            TileObject tileObject = Objects[layer];
            Objects.Remove(new KeyValuePair<TileLayer, TileObject>(layer, tileObject));

            ServerManager.Despawn(tileObject.GameObjectCache);
            tileObject.Destroy();
        }

        public bool IsLayerEmpty(TileLayer layer)
        {
            bool hasValue = Objects.TryGetValue(layer, out TileObject currentTileObject);
            bool isEmpty = currentTileObject == null;

            return !hasValue || isEmpty;
        }
    }
}