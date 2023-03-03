using FishNet.Object;
using SS3D.Core.Behaviours;
using SS3D.Systems.PlayerControl;
using UnityEngine;
using SS3D.Systems.Entities.Events;
using Coimbra.Services.Events;
using SS3D.Systems.PlayerControl.Events;
using SS3D.Logging;
using System.Collections.Generic;
using System.Linq;
using SS3D.Systems.Entities;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Core;
using SS3D.Data.Enums;

namespace SS3D.Systems.Roles
{
    public class RoleSystem : NetworkSystem
    {
        [SerializeField] private RolesAvailable rolesAvailable;
        private List<RoleCounter> roleCounters = new List<RoleCounter>();
        private Dictionary<Soul, RoleData> rolePlayers = new Dictionary<Soul, RoleData>();

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
            AddHandle(PlayerContainersReady.AddListener(HandlePlayerContainersReady));

            GetAvailableRoles();
        }

        /// <summary>
        /// Get all roles in the current AvailableRoles class and sets up
        /// the Role Counters for them
        /// </summary>
        [Server]
        private void GetAvailableRoles()
        {
            if (rolesAvailable == null)
            {
                Punpun.Panic(this, "Initial Available Roles not set!");
            }

            foreach (RolesData role in rolesAvailable.roles)
            {
                RoleCounter roleCounter = new RoleCounter();
                roleCounter.role = role.data;
                roleCounter.availableRoles = role.availableRoles;

                roleCounters.Add(roleCounter);
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

        [Server]
        private void HandlePlayerContainersReady(ref EventContext context, in PlayerContainersReady e)
        {
            GiveRoleLoadoutToPlayer(e.Player);
        }
        #endregion

        /// <summary>
        /// Assign a role to the player after joining the server
        /// </summary>
        /// <param name="soul"></param>
        private void AssignPlayerRole(Soul soul)
        {
            RoleCounter assistantRole = roleCounters.FirstOrDefault(rc => rc.role.Name == "Assistant");
            RoleCounter securityRole = roleCounters.FirstOrDefault(rc => rc.role.Name == "Security");

            if (securityRole == null || securityRole.currentRoles == securityRole.availableRoles)
            {
                assistantRole.AddPlayer(soul);
                rolePlayers.Add(soul, assistantRole.role);
            }
            else
            {
                securityRole.AddPlayer(soul);
                rolePlayers.Add(soul, securityRole.role);
            }
        }

        /// <summary>
        /// Remove players from the Role Counters if he quit before embarking
        /// </summary>
        /// <param name="soul"></param>
        private void RemovePlayerFromCounters(Soul soul)
        {
            KeyValuePair<Soul, RoleData>? rolePlayer =
                rolePlayers.FirstOrDefault(rp => rp.Key == soul);

            if (rolePlayer != null)
            {
                var roleData = rolePlayer.Value;
                var roleCounter = roleCounters.First(rc => rc.role == roleData.Value);

                roleCounter.RemovePlayer(soul);
            }
        }

        /// <summary>
        /// Checks the role of the player and spawns his items
        /// </summary>
        /// <param name="entity">The player that will receive the items</param>
        private void GiveRoleLoadoutToPlayer(Entity entity)
        {
            KeyValuePair<Soul, RoleData>? rolePlayer =
                rolePlayers.FirstOrDefault(rp => rp.Key == entity.Mind.Soul);

            if (rolePlayer != null)
            {
                var roleData = rolePlayer.Value;

                Punpun.Say(this, entity.Ckey + " embarked with role " + roleData.Value.Name);
                if (roleData.Value.Loadout != null)
                {
                    SpawnLoadoutItems(entity, roleData.Value.Loadout);
                }
            }
        }

        /// <summary>
        /// Spawn all the role items for the player
        /// </summary>
        /// <param name="entity">The player that will receive the items</param>
        /// <param name="loadout">The loadout of items he will receive</param>
        private void SpawnLoadoutItems(Entity entity, RoleLoadout loadout)
        {
            var hands = entity.GetComponent<Hands>();
            var inventory = entity.GetComponent<Inventory.Containers.Inventory>();

            SpawnItemInSlot(loadout.leftHandItem, loadout.leftHand, hands.HandContainers[0].Container);
            SpawnItemInSlot(loadout.rightHandItem, loadout.rightHand, hands.HandContainers[1].Container);
            SpawnItemInSlot(loadout.leftPocketItem, loadout.leftPocket, inventory.LeftPocketContainer);
            SpawnItemInSlot(loadout.rightPocketItem, loadout.rightPocket, inventory.RightPocketContainer);
        }

        /// <summary>
        /// Spawns an item inside a container slot after checking for boolean
        /// </summary>
        /// <param name="itemId">The id of the item to be spawned</param>
        /// <param name="shouldSpawn">Condition indicating if the item should be spawned</param>
        /// <param name="container">Container the item will be spawned in</param>
        private void SpawnItemInSlot(ItemId itemId, bool shouldSpawn, Container container)
        {
            if (!shouldSpawn)
            {
                return;
            }

            ItemSystem itemSystem = SystemLocator.Get<ItemSystem>();
            itemSystem.SpawnItemInContainer(itemId, container);
        }
    }
}