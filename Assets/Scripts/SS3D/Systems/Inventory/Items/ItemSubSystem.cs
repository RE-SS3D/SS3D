using FishNet.Object;
using SS3D.Core.Behaviours;
using SS3D.Data;
using SS3D.Data.Generated;
using SS3D.Logging;
using SS3D.Systems.Entities;
using SS3D.Systems.Inventory.Containers;
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
        public Item SpawnItem(string id, Vector3 position, Quaternion rotation)
        {
            Item itemPrefab = Assets.Get<GameObject>(AssetDatabases.Items, id).GetComponent<Item>();

            Item itemInstance = Instantiate(itemPrefab, position, rotation);
            ServerManager.Spawn(itemInstance.GameObject);

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
            SpawnItemInContainer(id.Name, attachedContainer);
        }

        /// <summary>
        /// Spawns an Item inside a container.
        ///
        /// TODO: Create a ItemSpawnOptions struct.
        /// </summary>
        /// <param name="id">The item ID to spawn.</param>
        /// <param name="container">The container to spawn into.</param>
        [Server]
        public Item SpawnItemInContainer(string id, AttachedContainer attachedContainer)
        {
            Item itemPrefab = Assets.Get<GameObject>(AssetDatabases.Items, id).GetComponent<Item>();

            if (attachedContainer is null)
            {
                Log.Error(this, "Container does not found!", Logs.ServerOnly);
                return null;
            }

            return SpawnItemInContainer(itemPrefab.GameObject, attachedContainer);
        }

        // <summary>
        /// Spawns an Item inside a container.
        ///
        /// TODO: Create a ItemSpawnOptions struct.
        /// </summary>
        /// <param name="id">The item ID to spawn.</param>
        /// <param name="container">The container to spawn into.</param>
        [Server]
        public Item SpawnItemInContainer(GameObject item, AttachedContainer attachedContainer)
        {
            Item itemInstance = Instantiate(item, Vector3.zero, Quaternion.identity).GetComponent<Item>();
            ServerManager.Spawn(itemInstance.GameObject);
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