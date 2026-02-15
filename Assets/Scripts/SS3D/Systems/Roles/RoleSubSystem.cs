using FishNet.Object;
using SS3D.Core.Behaviours;
using SS3D.Systems.PlayerControl;
using UnityEngine;
using Coimbra.Services.Events;
using SS3D.Systems.PlayerControl.Events;
using SS3D.Logging;
using System.Collections.Generic;
using System.Linq;
using SS3D.Systems.Entities;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Core;
using SS3D.Systems.Inventory.Items.Generic;

namespace SS3D.Systems.Roles
{
    public class RoleSubSystem : NetworkSubSystem
    {
        [SerializeField] private RolesAvailable _rolesAvailable;
        private List<RoleCounter> _roleCounters = new List<RoleCounter>();
        private Dictionary<Player, RoleData> _rolePlayers = new Dictionary<Player, RoleData>();

        #region Setup
        protected override void OnStart()
        {
            base.OnStart();
            Setup();
        }

        [Server]
        private void Setup()
        {
            AddHandle(OnlinePlayersChanged.AddListener(HandleOnlinePlayersChanged));

            GetAvailableRoles();
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
                RoleCounter roleCounter = new RoleCounter();
                roleCounter.Role = role.Data;
                roleCounter.AvailableRoles = role.AvailableRoles;

                _roleCounters.Add(roleCounter);
            }
        }
        #endregion

        #region Event Handlers
        [Server]
        private void HandleOnlinePlayersChanged(ref EventContext context, in OnlinePlayersChanged e)
        {
            if (!e.AsServer)
            {
                return;
            }

            if (e.ChangeType == ChangeType.Addition)
            {
                HandlePlayerJoined(e.ChangedPlayer);
            } else 
            if (e.ChangeType == ChangeType.Removal)
            {
                HandlePlayerLeft(e.ChangedPlayer);
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
        #endregion

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
        /// Checks the role of the player and spawns his items
        /// </summary>
        /// <param name="entity">The player that will receive the items</param>
        [ServerRpc(RequireOwnership = false)]
        public void GiveRoleLoadoutToPlayer(Entity entity)
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
        /// Spawn the player's PDA and IDCard with the proper permissions
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="role"></param>
        private void SpawnIdentificationItems(Entity entity, RoleData role)
        {
            ItemSubSystem itemSystem = SubSystems.Get<ItemSubSystem>();
            HumanInventory inventory = entity.GetComponent<HumanInventory>();

            if (!inventory.TryGetTypeContainer(ContainerType.Identification, 0, out AttachedContainer container)) return;

            Item pdaItem = itemSystem.SpawnItemInContainer(role.PDAPrefab, container);
            Item idCardItem = itemSystem.SpawnItem(role.IDCardPrefab.name, Vector3.zero, Quaternion.identity);

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
            Hands hands = entity.GetComponent<Hands>();
            HumanInventory inventory = entity.GetComponent<HumanInventory>();

            Dictionary<ContainerType, AttachedContainer> containers = new Dictionary<ContainerType, AttachedContainer>();
            List<AttachedContainer> handContainers = hands.HandContainers;

            foreach (AttachedContainer inventoryContainer in inventory.Containers)
            {
                if (inventoryContainer.ContainerType == ContainerType.Hand)
                {
                    continue;
                }

                containers.Add(inventoryContainer.ContainerType, inventoryContainer);
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
            

            inventory.TriggerInventorySetup();
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

            ItemSubSystem itemSystem = SubSystems.Get<ItemSubSystem>();
            itemSystem.SpawnItemInContainer(itemId, container);
        }
    }
}