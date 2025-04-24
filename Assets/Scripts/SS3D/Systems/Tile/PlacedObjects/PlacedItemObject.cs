using FishNet;
using FishNet.Object;
using SS3D.Core;
using SS3D.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Component which is added to every item that is part of the tilemap.
    /// </summary>
    public class PlacedItemObject : NetworkBehaviour
    {
        /// <summary>
        /// Creates a new PlacedItemObject from a prefab at a given position and rotation. Uses NetworkServer.Spawn() if a server is running.
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <param name="origin"></param>
        /// <param name="rotation"></param>
        /// <param name="itemSo"></param>
        /// <returns></returns>
        public static PlacedItemObject Create(Vector3 worldPosition, Quaternion rotation, ItemObjectSo itemSo)
        {
            GameObject placedGameObject = Instantiate(itemSo.prefab);
            placedGameObject.transform.SetPositionAndRotation(worldPosition, rotation);

            PlacedItemObject placedObject = placedGameObject.GetComponent<PlacedItemObject>();
            if (placedObject == null)
            {
                // Ideally an editor script adds this instead of doing it at runtime
                placedObject = placedGameObject.AddComponent<PlacedItemObject>();
            }

            placedObject.Setup(worldPosition, rotation, itemSo);

            if (InstanceFinder.ServerManager != null && placedObject.GetComponent<NetworkObject>() != null)
            {
                if (placedObject.GetComponent<NetworkObject>() == null)
                    Log.Warning(SubSystems.Get<TileSubSystem>(), "{placedObject} does not have a Network Component and will not be spawned",
                        Logs.Generic, placedObject.NameString);
                else
                    InstanceFinder.ServerManager.Spawn(placedGameObject);
            }

            return placedObject;
        }

        private ItemObjectSo _itemSo;
        private Vector3 _worldPosition;
        private Quaternion _rotation;

        public string NameString => _itemSo.NameString;

        /// <summary>
        /// Set up a new item object.
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <param name="rotation"></param>
        /// <param name="itemSo"></param>
        public void Setup(Vector3 worldPosition, Quaternion rotation, ItemObjectSo itemSo)
        {
            _worldPosition = worldPosition;
            _rotation = rotation;
            _itemSo = itemSo;
        }

        /// <summary>
        /// Destroys itself.
        /// </summary>
        public void DestroySelf()
        {
            InstanceFinder.ServerManager.Despawn(gameObject);
        }

        /// <summary>
        /// Returns a new SaveObject for use in saving/loading.
        /// </summary>
        /// <returns></returns>
        public SavedPlacedItemObject Save()
        {
            return new SavedPlacedItemObject
            {
                itemName = _itemSo.NameString,
                worldPosition = _worldPosition,
                rotation = _rotation,
            };
        }
    }
}