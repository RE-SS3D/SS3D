using FishNet.Object;
using SS3D.Core.Behaviours;
using SS3D.Systems.PlayerControl;
using UnityEngine;
using Coimbra.Services.Events;
using JetBrains.Annotations;
using SS3D.Systems.PlayerControl.Events;
using SS3D.Logging;
using System.Collections.Generic;
using System.Linq;
using SS3D.Systems.Entities;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Core;
using SS3D.Data.Enums;
using SS3D.Systems.Inventory.Items.Generic;
using SS3D.Systems.Inventory.Items.Identification;

namespace SS3D.Systems.Roles
{
    public sealed class RoleSubsystem : NetworkSubsystem
    {
        [SerializeField] private RolesAvailable _rolesAvailable;

        private readonly List<RoleCounter> _roleCounters = new();
        private readonly Dictionary<Soul, RoleData> _rolePlayers = new();

        #region Setup
        protected override void OnStart()
        {
            base.OnStart();
            Setup();
        }

        [Server]
        private void Setup()
        {
            AddHandle(OnlineSoulsChanged.AddListener(HandleOnlineSoulsChanged));

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
                Punpun.Error(this, "Initial Available Roles not set!");
            }

            foreach (RolesData role in _rolesAvailable.Roles)
            {
                RoleCounter roleCounter = new()
                {
                    Role = role.Data,
                    AvailableRoles = role.AvailableRoles,
                };

                _roleCounters.Add(roleCounter);
            }
        }
        #endregion

        #region Event Handlers
        [Server]
        private void HandleOnlineSoulsChanged(ref EventContext context, in OnlineSoulsChanged e)
        {
            if (!e.AsServer)
            {
                return;
            }

            if (e.ChangeType == ChangeType.Addition)
            {
                HandlePlayerJoined(e.ChangedSoul);
            } else 
            if (e.ChangeType == ChangeType.Removal)
            {
                HandlePlayerLeft(e.ChangedSoul);
            }
        }

        [Server]
        private void HandlePlayerJoined(Soul soul)
        {
            AssignPlayerRole(soul);
        }

        [Server]
        private void HandlePlayerLeft(Soul soul)
        {
            RemovePlayerFromCounters(soul);
        }
        #endregion

        /// <summary>
        /// Assign a role to the player after joining the server
        /// </summary>
        /// <param name="soul"></param>
        private void AssignPlayerRole([NotNull] Soul soul)
        {
            if (_rolePlayers.TryGetValue(soul, out _))
            {
                return;
            }

            RoleCounter assistantRole = _roleCounters.FirstOrDefault(rc => rc.Role.Name == "Assistant");

            RoleCounter securityRole = _roleCounters.FirstOrDefault(rc => rc.Role.Name == "Security");
            if (securityRole == null || securityRole.CurrentRoles == securityRole.AvailableRoles)
            {
                assistantRole.AddPlayer(soul);
                _rolePlayers.Add(soul, assistantRole.Role);
            }
            else
            {
                securityRole.AddPlayer(soul);
                _rolePlayers.Add(soul, securityRole.Role);
            }
        }

        /// <summary>
        /// Remove players from the Role Counters if he quit before embarking
        /// </summary>
        /// <param name="soul"></param>
        private void RemovePlayerFromCounters(Soul soul)
        {
            KeyValuePair<Soul, RoleData>? rolePlayer =
                _rolePlayers.FirstOrDefault(rp => rp.Key == soul);

            RoleData roleData = rolePlayer.Value.Value;

            if (_roleCounters.All(rc => rc.Role != roleData))
            {
                return;
            }

            RoleCounter roleCounter = _roleCounters.First(rc => rc.Role == roleData);

            roleCounter.RemovePlayer(soul);
        }

        /// <summary>
        /// Checks the role of the player and spawns his items
        /// </summary>
        /// <param name="entity">The player that will receive the items</param>
        [ServerRpc(RequireOwnership = false)]
        public void GiveRoleLoadoutToPlayer(Entity entity)
        {
            KeyValuePair<Soul, RoleData>? rolePlayer = _rolePlayers.FirstOrDefault(rp => rp.Key == entity.Mind.Soul);

            RoleData roleData = rolePlayer.Value.Value;

            if (roleData.Loadout != null)
            {
                Punpun.Information(this, entity.Ckey + " embarked with role " + roleData.Name);
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
            ItemSubsystem itemSystem = Subsystems.Get<ItemSubsystem>();
            Inventory.Containers.Inventory inventory = entity.GetComponent<Inventory.Containers.Inventory>();

            Item pdaItem = itemSystem.SpawnItemInContainer(role.PDAPrefab, inventory.IDContainer);
            Item idCardItem = itemSystem.SpawnItem(role.IDCardPrefab, Vector3.zero, Quaternion.identity);

            Pda pda = (Pda)pdaItem;
            IDCard idCard = (IDCard)idCardItem;


            // Set up ID Card data
            idCard.OwnerName = entity.Ckey;
            idCard.RoleName = role.Name;
            foreach (IDPermission permission in role.Permissions)
            {
                idCard.AddPermission(permission);
                Punpun.Information(this, "Added " + permission.Name + " permission to IDCard of " + entity.Ckey);
            }

            pda.IDCard = idCardItem;
        }

        /// <summary>
        /// Spawn all the role items for the player
        /// </summary>
        /// <param name="entity">The player that will receive the items</param>
        /// <param name="loadout">The loadout of items he will receive</param>
        private void SpawnLoadoutItems(Entity entity, RoleLoadout loadout)
        {
            Hands hands = entity.GetComponent<Hands>();
            Inventory.Containers.Inventory inventory = entity.GetComponent<Inventory.Containers.Inventory>();

            SpawnItemInSlot(loadout.LeftHandItem, loadout.LeftHand, hands.HandContainers[0]);
            SpawnItemInSlot(loadout.RightHandItem, loadout.RightHand, hands.HandContainers[1]);
            SpawnItemInSlot(loadout.LeftPocketItem, loadout.LeftPocket, inventory.LeftPocketContainer);
            SpawnItemInSlot(loadout.RightPocketItem, loadout.RightPocket, inventory.RightPocketContainer);
        }

        /// <summary>
        /// Spawns an item inside a container slot after checking for boolean
        /// </summary>
        /// <param name="itemId">The id of the item to be spawned</param>
        /// <param name="shouldSpawn">Condition indicating if the item should be spawned</param>
        /// <param name="container">Container the item will be spawned in</param>
        private void SpawnItemInSlot(ItemId itemId, bool shouldSpawn, AttachedContainer container)
        {
            if (!shouldSpawn)
            {
                return;
            }

            ItemSubsystem itemSystem = Subsystems.Get<ItemSubsystem>();
            itemSystem.SpawnItemInContainer(itemId, container);
        }
    }
}