using System;
using System.Collections.Generic;
using FishNet.Object;
using SS3D.Tilemaps.Enums;
using SS3D.Tilemaps.Objects;
using SS3D.Utils;
using UnityEngine;

namespace SS3D.Tilemaps
{
    /// <summary>
    /// A tile data, used to abstract that information and avoid using network objects for the tile system.
    /// </summary>
    [Serializable]
    public struct TileData
    {
        /// <summary>
        /// Called whenever a field is changed in this tile.
        /// </summary>
        public event Action<TileData, ChangeType, TileObjectLayer, TileObject> OnTileObjectChanged;

        /// <summary>
        /// The objects inside this tile.
        /// </summary>
        public readonly Dictionary<TileObjectLayer, TileObject> Objects;

        /// <summary>
        /// The position of this tile.
        /// </summary>
        public readonly Vector3Int TilePosition;

        public TileData(Vector3Int tilePosition) : this()
        {
            Objects = new Dictionary<TileObjectLayer, TileObject>();
            TilePosition = tilePosition;
        }

        /// <summary>
        /// Adds a tile object at that layer.
        /// </summary>
        /// <param name="objectLayer"></param>
        /// <param name="tileObject"></param>
        public void AddObjectAt(TileObjectLayer objectLayer, TileObject tileObject)
        {
            Objects.Add(objectLayer, tileObject);

            OnTileObjectChanged?.Invoke(this, ChangeType.Addition, objectLayer, tileObject);
        }

        /// <summary>
        /// Destroys (or removes) a tile object at a layer.
        /// </summary>
        /// <param name="objectLayer"></param>
        [Server]
        public void DestroyObjectAt(TileObjectLayer objectLayer)
        {
            TileObject tileObject = Objects[objectLayer];

            OnTileObjectChanged?.Invoke(this, ChangeType.Removal, objectLayer, tileObject);
        }

        /// <summary>
        /// Checks if a tile object layer is empty.
        /// </summary>
        /// <param name="objectLayer"></param>
        /// <returns>Is the layer empty?</returns>
        public bool IsLayerEmpty(TileObjectLayer objectLayer)
        {
            bool hasValue = Objects.TryGetValue(objectLayer, out TileObject currentTileObject);
            bool isEmpty = currentTileObject == null;

            return !hasValue || isEmpty;
        }
    }
}