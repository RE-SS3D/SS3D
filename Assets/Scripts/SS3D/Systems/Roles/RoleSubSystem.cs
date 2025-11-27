using Coimbra.Services.Events;
using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Systems.Entities;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Inventory.Items.Generic;
using SS3D.Systems.Inventory.UI;
using SS3D.Systems.PlayerControl;
using SS3D.Systems.PlayerControl.Events;
using SS3D.Traits;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Roles
{
    public class RoleSubSystem : NetworkSubSystem
    {
        private readonly List<RoleCounter> _roleCounters = new();

        private readonly Dictionary<Player, RoleData> _rolePlayers = new();

        [SerializeField]
        private RolesAvailable _rolesAvailable;

        protected override void OnStart()
        {
            base.OnStart();
            Setup();
        }

        [Server]
        private void Setup()
        {
            AddHandle(OnlinePlayersChanged.AddListener(HandleOnlinePlayersChanged));
            Subsystems.Get<EntitySubSystem>().EntitySpawned += GiveRoleLoadoutToPlayer;
            GetAvailableRoles();
        }

        /// <summary>
        /// Checks the role of the player and spawns his items
        /// </summary>
        /// <param name="entity">The player that will receive the items</param>
        [ServerRpc(RequireOwnership = false)]
        private void GiveRoleLoadoutToPlayer(Entity entity)
        {
            KeyValuePair<Player, RoleData>? rolePlayer =
                _rolePlayers.FirstOrDefault(rp => rp.Key == entity.Mind.player);

            if (rolePlayer != null)
            {
                RoleData roleData = rolePlayer.Value.Value;

                Log.Information(this, entity.Ckey + " embarked with role " + roleData.Name);
                SpawnIdentificationItems(entity, roleData);

                if (roleData.Loadout != null)
                {
                    SpawnLoadoutItems(entity, roleData.Loadout);
                }
            }
        }

        /// <summary>
        /// Get all roles in the current AvailableRoles class and sets up
        /// the Role Counters for them
        /// </summary>
        [Server]
        private void GetAvailableRoles()
        {
            if (_rolesAvailable == null)
            {
                Log.Error(this, "Initial Available Roles not set!");
            }

            foreach (RolesData role in _rolesAvailable.Roles)
            {
                RoleCounter roleCounter = new(role.Data, role.AvailableRoles);
                _roleCounters.Add(roleCounter);
            }
        }

        [Server]
        private void HandleOnlinePlayersChanged(ref EventContext context, in OnlinePlayersChanged e)
        {
            if (!e.AsServer)
            {
                return;
            }

            switch (e.ChangeType)
            {
                case ChangeType.Addition:
                {
                    HandlePlayerJoined(e.ChangedPlayer);
                    break;
                }

                case ChangeType.Removal:
                {
                    HandlePlayerLeft(e.ChangedPlayer);
                    break;
                }

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        [Server]
        private void HandlePlayerJoined(Player player)
        {
            AssignPlayerRole(player);
        }

        [Server]
        private void HandlePlayerLeft(Player player)
        {
            RemovePlayerFromCounters(player);
        }

        /// <summary>
        /// Assign a role to the player after joining the server
        /// </summary>
        /// <param name="player</param>
        private void AssignPlayerRole(Player player)
        {
            RoleCounter assistantRole = _roleCounters.FirstOrDefault(rc => rc.Role.Name == "Assistant");
            RoleCounter securityRole = _roleCounters.FirstOrDefault(rc => rc.Role.Name == "Security");

            if (securityRole == null || securityRole.CurrentRoles == securityRole.AvailableRoles)
            {
                assistantRole.AddPlayer(player);
                _rolePlayers.Add(player, assistantRole.Role);
            }
            else
            {
                securityRole.AddPlayer(player);
                _rolePlayers.Add(player, securityRole.Role);
            }
        }

        /// <summary>
        /// Remove players from the Role Counters if he quit before embarking
        /// </summary>
        /// <param name="player</param>
        private void RemovePlayerFromCounters(Player player)
        {
            KeyValuePair<Player, RoleData>? rolePlayer =
                _rolePlayers.FirstOrDefault(rp => rp.Key == player);

            if (rolePlayer != null)
            {
                RoleData roleData = rolePlayer.Value.Value;
                RoleCounter roleCounter = _roleCounters.First(rc => rc.Role == roleData);

                roleCounter.RemovePlayer(player);
            }
        }

        /// <summary>
        /// Spawn the player's PDA and IDCard with the proper permissions
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="role"></param>
        private void SpawnIdentificationItems(Entity entity, RoleData role)
        {
            ItemSubSystem itemSubSystem = Subsystems.Get<ItemSubSystem>();
            IInventory inventory = entity.GetComponent<IInventory>();

            if (!inventory.TryGetTypeContainer(ContainerType.Identification, 0, out AttachedContainer container))
            {
                return;
            }

            Item pdaItem = itemSubSystem.SpawnItemInContainer(role.PdaPrefab, container);
            Item idCardItem = itemSubSystem.SpawnItem(role.IDCardPrefab.name, Vector3.zero, Quaternion.identity);

            PDA pda = (PDA)pdaItem;
            IDCard idCard = (IDCard)idCardItem;

            // Set up ID Card data
            idCard.OwnerName = entity.Ckey;
            idCard.RoleName = role.Name;
            foreach (IDPermission permission in role.Permissions)
            {
                idCard.AddPermission(permission);
                Log.Information(this, "Added " + permission.Name + " permission to IDCard of " + entity.Ckey);
            }

            pda.StartingIDCard = idCardItem;
        }

        /// <summary>
        /// Spawn all the role items for the player
        /// </summary>
        /// <param name="entity">The player that will receive the items</param>
        /// <param name="loadout">The loadout of items he will receive</param>
        private void SpawnLoadoutItems(Entity entity, RoleLoadout loadout)
        {
            IInventory inventory = entity.GetComponent<IInventory>();

            Dictionary<ContainerType, AttachedContainer> containers = new();
            List<AttachedContainer> handContainers = new();

            foreach (AttachedContainer inventoryContainer in inventory.Containers)
            {
                if (inventoryContainer.ContainerType == ContainerType.Hand)
                {
                    handContainers.Add(inventoryContainer);
                }
                else
                {
                    containers.Add(inventoryContainer.ContainerType, inventoryContainer); 
                }
            }

            foreach (KeyValuePair<ContainerType, GameObject> itemToEquip in loadout.Equipment)
            {
                if (itemToEquip.Value == null)
                {
                    continue;
                }

                if (containers.TryGetValue(itemToEquip.Key, out AttachedContainer container))
                {
                    SpawnItemInSlot(itemToEquip.Value, true, container);
                }
            }

            if (loadout.HandLeft != null)
            {
                SpawnItemInSlot(loadout.HandLeft, true, handContainers[0]);
            }

            if (loadout.HandRight != null)
            {
                SpawnItemInSlot(loadout.HandRight, true, handContainers[1]);
            }
            
            inventory.Init();
        }

        /// <summary>
        /// Spawns an item inside a container slot after checking for boolean
        /// </summary>
        /// <param name="itemId">The id of the item to be spawned</param>
        /// <param name="shouldSpawn">Condition indicating if the item should be spawned</param>
        /// <param name="container">Container the item will be spawned in</param>
        private void SpawnItemInSlot(GameObject itemId, bool shouldSpawn, AttachedContainer container)
        {
            if (!shouldSpawn)
            {
                return;
            }

            ItemSubSystem itemSubSystem = Subsystems.Get<ItemSubSystem>();
            itemSubSystem.SpawnItemInContainer(itemId, container);
        }
    }
}
