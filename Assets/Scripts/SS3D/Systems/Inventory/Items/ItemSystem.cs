using System.Collections.Generic;
using FishNet.Object;
using SS3D.Core.Behaviours;
using SS3D.Data;
using SS3D.Data.AssetDatabases;
using SS3D.Data.Enums;
using SS3D.Logging;
using SS3D.Systems.Entities;
using SS3D.Systems.Inventory.Containers;
using UnityEngine;

namespace SS3D.Systems.Inventory.Items
{
    /// <summary>
    /// System used to spawn items.
    /// </summary>
    public sealed class ItemSystem : NetworkSystem
    {
        /// <summary>
        /// A dictionary of all the preloaded prefabs using the ItemIDs as key.
        /// </summary>
        private readonly Dictionary<ItemId, Item> _itemPrefabs = new();

        protected override void OnStart()
        {
            base.OnStart();

            LoadItemPrefabs();
        }

        /// <summary>
        /// Loads the item prefabs into memory with the item id and setting up the item ids variable in the items.
        /// </summary>
        private void LoadItemPrefabs()
        {
            AssetDatabase items = Assets.GetDatabase((int)AssetDatabases.Items);

            for (int index = 0; index < items.Assets.Count; index++)
            {
                ItemId id = (ItemId)index;

                GameObject itemObject = Assets.Get(id);
                if(itemObject.TryGetComponent(out Item item))
                {
                    item.ItemId = id;
                    _itemPrefabs.Add(id, item);
                }
                else
                {
                    Punpun.Error(this, $"gameobject {itemObject} doesn't have any item component");
                } 
            }
        }

        /// <summary>
        /// Requests to spawn an item.
        /// </summary>
        /// <param name="id">The item ID to spawn.</param>
        /// <param name="position">The desired position to spawn.</param>
        /// <param name="rotation">The desired rotation to apply.</param>
        [ServerRpc(RequireOwnership = false)]
        public void CmdSpawnItem(ItemId id, Vector3 position, Quaternion rotation)
        {
            SpawnItem(id, position, rotation);
        }

        /// <summary>
        /// Spawns an Item at a position and rotation.
        ///
        /// TODO: Create a ItemSpawnOptions struct.
        /// </summary>
        /// <param name="id">The item ID to spawn.</param>
        /// <param name="position">The desired position to spawn.</param>
        /// <param name="rotation">The desired rotation to apply.</param>
        [Server]
        public Item SpawnItem(ItemId id, Vector3 position, Quaternion rotation)
        {
            bool hasValue = _itemPrefabs.TryGetValue(id, out Item itemPrefab);

            if (!hasValue)
            {
                Punpun.Error(this, "No item with ID {id} was found", Logs.ServerOnly, id.ToString());
                return null;
            }

            Item itemInstance = Instantiate(itemPrefab, position, rotation);
            ServerManager.Spawn(itemInstance.GameObject);

            Punpun.Information(this, "Item {itemInstance} spawned at {position}", Logs.ServerOnly, itemInstance.name, position);
            return itemInstance;
        }


        /// <summary>
        /// Requests to spawn an item in a given container.
        /// </summary>
        /// <param name="id">The item ID to spawn.</param>
        /// <param name="position">The desired position to spawn.</param>
        /// <param name="rotation">The desired rotation to apply.</param>
        [ServerRpc(RequireOwnership = false)]
        public void CmdSpawnItemInContainer(ItemId id, AttachedContainer attachedContainer)
        {
            SpawnItemInContainer(id, attachedContainer);
        }


        /// <summary>
        /// Spawns an Item inside a container.
        ///
        /// TODO: Create a ItemSpawnOptions struct.
        /// </summary>
        /// <param name="id">The item ID to spawn.</param>
        /// <param name="container">The container to spawn into.</param>
        [Server]
        public Item SpawnItemInContainer(ItemId id, AttachedContainer attachedContainer)
        {
            bool hasValue = _itemPrefabs.TryGetValue(id, out Item itemPrefab);

            if (!hasValue)
            {
                Punpun.Error(this, "No item with ID {id} was found", Logs.ServerOnly, id.ToString());
                return null;
            }

            if (attachedContainer is null)
            {
                Punpun.Error(this, "Container does not found!", Logs.ServerOnly);
                return null;
            }

            Item itemInstance = Instantiate(itemPrefab, Vector3.zero, Quaternion.identity);
            ServerManager.Spawn(itemInstance.GameObject);
            attachedContainer.AddItem(itemInstance);

            Punpun.Information(this, "Item {item} spawned in container {container}", Logs.ServerOnly, itemInstance.name, attachedContainer.ContainerName);
            return itemInstance;
        }

        /// <summary>
        /// Return the item in the active hand of the given player entity.
        /// </summary>
        public Item GetItemInHand(Entity playerEntity)
        {
            Hands hands = playerEntity.GetComponentInParent<HumanInventory>().Hands;
            return hands.SelectedHand.ItemInHand;
        }


    }
}