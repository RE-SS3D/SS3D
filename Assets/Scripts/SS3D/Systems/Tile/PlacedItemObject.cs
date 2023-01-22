using FishNet;
using FishNet.Object;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile
{
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
                placedObject = placedGameObject.AddComponent<PlacedItemObject>();
            }

            placedObject.Setup(worldPosition, rotation, itemSo);

            if (InstanceFinder.ServerManager != null && placedObject.GetComponent<NetworkObject>() != null)
            {
                InstanceFinder.ServerManager.Spawn(placedGameObject);
            }

            return placedObject;
        }


        /// <summary>
        /// SaveObject that contains all information required to reconstruct the object.
        /// </summary>
        [Serializable]
        public class PlacedSaveObject
        {
            public string itemName;
            public Vector3 worldPosition;
            public Quaternion rotation;
        }

        /// <summary>
        /// Returns a new SaveObject for use in saving/loading.
        /// </summary>
        /// <returns></returns>
        public PlacedSaveObject Save()
        {
            return new PlacedSaveObject
            {
                itemName = _itemSo.nameString,
                worldPosition = _worldPosition,
                rotation = _rotation,
            };
        }

        private ItemObjectSo _itemSo;
        private Vector3 _worldPosition;
        private Quaternion _rotation;

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
            base.Despawn();
        }
    }
}