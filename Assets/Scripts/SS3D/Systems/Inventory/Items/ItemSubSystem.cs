using FishNet.Connection;
using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Data;
using SS3D.Data.Generated;
using SS3D.Logging;
using SS3D.Permissions;
using SS3D.Systems.Entities;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.PlayerControl;
using UnityEngine;

namespace SS3D.Systems.Inventory.Items
{
    /// <summary>
    /// System used to spawn items.
    /// </summary>
    public sealed class ItemSubSystem : NetworkSubSystem
    {
        /// <summary>
        /// Requests to spawn an item. Administrators only; server code calls <see cref="SpawnItem"/>.
        /// </summary>
        /// <param name="id">The item ID to spawn.</param>
        /// <param name="position">The desired position to spawn.</param>
        /// <param name="rotation">The desired rotation to apply.</param>
        [ServerRpc(RequireOwnership = false)]
        public void CmdSpawnItem(string id, Vector3 position, Quaternion rotation, NetworkConnection conn = null)
        {
            if (!RpcInput.IsInStation(position) || !IsAdministrator(conn))
            {
                return;
            }

            SpawnItem(id, position, rotation);
        }

        private static bool IsAdministrator(NetworkConnection conn)
        {
            bool allowed = conn != null && SubSystems.TryGet(out PlayerSubSystem players)
                && SubSystems.TryGet(out PermissionSubSystem permissions)
                && permissions.IsAtLeast(players.GetCkey(conn), ServerRoleTypes.Administrator);
            if (!allowed)
            {
                Log.Warning(typeof(ItemSubSystem), "Refused an item spawn request from a non-admin connection {clientId}",
                    Logs.ServerOnly, conn?.ClientId ?? -1);
            }

            return allowed;
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
            Item itemPrefab = Assets.Get<Item>(AssetDatabases.Items, id);

            // Instantiate(..., identity) would wipe authored side-lying pitch/roll on long items.
            Quaternion spawnRotation = rotation;
            if (rotation == Quaternion.identity && itemPrefab != null)
            {
                spawnRotation = itemPrefab.WorldRestRotation;
            }

            Item itemInstance = Instantiate(itemPrefab, position, spawnRotation);
            ServerManager.Spawn(itemInstance.GameObject);

            Log.Information(this, "Item {itemInstance} spawned at {position}", Logs.ServerOnly, itemInstance.name, position);
            return itemInstance;
        }


        /// <summary>
        /// Requests to spawn an item in a given container. Administrators only; server code calls
        /// <see cref="SpawnItemInContainer"/>.
        /// </summary>
        /// <param name="id">The item ID to spawn.</param>
        /// <param name="position">The desired position to spawn.</param>
        /// <param name="rotation">The desired rotation to apply.</param>
        [ServerRpc(RequireOwnership = false)]
        public void CmdSpawnItemInContainer(Item id, AttachedContainer attachedContainer, NetworkConnection conn = null)
        {
            if (id == null || !IsAdministrator(conn))
            {
                return;
            }

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
            Item itemPrefab = Assets.Get<Item>(AssetDatabases.Items, id);

            if (attachedContainer is not null && itemPrefab is not null)
            {
                return SpawnItemInContainer(itemPrefab.GameObject, attachedContainer);
            }

            Log.Error(this, "Container does not found!", Logs.ServerOnly);
            return null;

        }

        // <summary>
        /// Spawns an Item inside a container.
        ///
        /// TODO: Create a ItemSpawnOptions struct.
        /// </summary>
        /// <param name="id">The item ID to spawn.</param>
        /// <param name="container">The container to spawn into.</param>
        /// <returns>The spawned item, or null when the container refused it (the item is despawned).</returns>
        [Server]
        public Item SpawnItemInContainer(GameObject item, AttachedContainer attachedContainer)
        {
            Item itemInstance = Instantiate(item, Vector3.zero, Quaternion.identity).GetComponent<Item>();
            ServerManager.Spawn(itemInstance.GameObject);
            if (!attachedContainer.AddItem(itemInstance))
            {
                // Don't leave a refused item lying at the spawn point (the world origin).
                Log.Warning(this, "Item {item} does not fit in container {container}", Logs.ServerOnly, itemInstance.name, attachedContainer.ContainerName);
                ServerManager.Despawn(itemInstance.GameObject);
                return null;
            }

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