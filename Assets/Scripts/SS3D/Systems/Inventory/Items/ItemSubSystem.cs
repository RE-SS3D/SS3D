using FishNet.Object;
using JetBrains.Annotations;
using SS3D.Core.Behaviours;
using SS3D.Data;
using SS3D.Data.Generated;
using SS3D.Data.Networking;
using SS3D.Logging;
using SS3D.Systems.Entities;
using SS3D.Systems.Inventory.Containers;
using System.Threading.Tasks;
using UnityEngine;

namespace SS3D.Systems.Inventory.Items
{
    /// <summary>
    /// System used to spawn items.
    /// </summary>
    public sealed class ItemSubSystem : NetworkSubSystem
    {
        /// <summary>
        /// Requests to spawn an item.
        /// </summary>
        /// <param name="id">The item ID to spawn.</param>
        /// <param name="position">The desired position to spawn.</param>
        /// <param name="rotation">The desired rotation to apply.</param>
        [ServerRpc(RequireOwnership = false)]
        public void CmdSpawnItem(string id, Vector3 position, Quaternion rotation)
        {
            SpawnItemAsync(id, position, rotation);
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
        [ItemCanBeNull]
        public async Task<Item> SpawnItemAsync(string id, Vector3 position, Quaternion rotation)
        {
            AssetHandle<Item> itemHandle = await new AssetRequest<Item>(id).LoadAsync();

            if (!itemHandle)
            {
                Log.Error(this, "Item with id {id} not found in database!", Logs.ServerOnly, id);

                return null;
            }

            Item itemInstance = Instantiate(itemHandle.Asset, position, rotation);
            await NetworkSpawner.SpawnAsync(itemInstance, id);

            Log.Information(this, "Item {itemInstance} spawned at {position}", Logs.ServerOnly, itemInstance.name, position);

            return itemInstance;
        }

        /// <summary>
        /// Requests to spawn an item in a given container.
        /// </summary>
        /// <param name="id">The item ID to spawn.</param>
        /// <param name="position">The desired position to spawn.</param>
        /// <param name="rotation">The desired rotation to apply.</param>
        [ServerRpc(RequireOwnership = false)]
        public void CmdSpawnItemInContainer(Item id, AttachedContainer attachedContainer)
        {
            SpawnItemInContainerAsync(id.Name, attachedContainer);
        }

        /// <summary>
        /// Spawns an Item inside a container.
        ///
        /// TODO: Create a ItemSpawnOptions struct.
        /// </summary>
        /// <param name="id">The item ID to spawn.</param>
        /// <param name="container">The container to spawn into.</param>
        [Server]
        public async Task<Item> SpawnItemInContainerAsync(string id, AttachedContainer attachedContainer)
        {
            AssetHandle<Item> itemHandle = await new AssetRequest<Item>(id).LoadAsync();

            if (!itemHandle)
            {
                Log.Error(this, "Item with id {id} not found in database!", Logs.ServerOnly, id);
                return null;
            }
            
            Item itemPrefab = itemHandle.Asset;

            if (attachedContainer is not null && itemPrefab is not null)
            {
                return await SpawnItemInContainerAsync(itemPrefab.GameObject, id, attachedContainer);
            }

            Log.Error(this, "Container does not found!", Logs.ServerOnly);
            return null;

        }

        // <summary>
        /// Spawns an Item inside a container.
        /// 
        /// TODO: Create a ItemSpawnOptions struct.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="key"></param>
        /// <param name="attachedContainer"></param>
        /// <param name="id">The item ID to spawn.</param>
        /// <param name="container">The container to spawn into.</param>
        [Server]
        public async Task<Item> SpawnItemInContainerAsync(GameObject item, string key, AttachedContainer attachedContainer)
        {
            Item itemInstance = Instantiate(item, Vector3.zero, Quaternion.identity).GetComponent<Item>();

            await NetworkSpawner.SpawnAsync(itemInstance, key);
            
            attachedContainer.AddItem(itemInstance);

            Log.Information(this, "Item {item} spawned in container {container}", Logs.ServerOnly, itemInstance.name, attachedContainer.ContainerName);
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