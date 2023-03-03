using FishNet.Object;
using SS3D.Core.Behaviours;
using SS3D.Systems.Rounds.Events;
using SS3D.Systems.Rounds.Messages;
using SS3D.Systems.PlayerControl;
using UnityEngine;
using SS3D.Systems.Entities.Events;
using Coimbra.Services.Events;
using SS3D.Systems.PlayerControl.Events;
using SS3D.Logging;
using System.Collections.Generic;
using System.Linq;
using SS3D.Systems.Entities;
using System;

namespace SS3D.Systems.Roles
{
    public class RoleSystem : NetworkSystem
    {
        [SerializeField] private RolesAvailable rolesAvailable;
        private List<RoleCounter> roleCounters = new List<RoleCounter>();
        private Dictionary<Soul, RoleData> rolePlayers = new Dictionary<Soul, RoleData>();

        protected override void OnStart()
        {
            base.OnStart();
            Setup();
        }

        [Server]
        private void Setup()
        {
            AddHandle(IndividualPlayerEmbarked.AddListener(HandleIndividualPlayerEmbarked));
            AddHandle(OnlineSoulsChanged.AddListener(HandleOnlineSoulsChanged));

            GetAvailableRoles();
        }

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
        private void HandleIndividualPlayerEmbarked(ref EventContext context, in IndividualPlayerEmbarked e)
        {
            GiveRoleLoadoutToPlayer(e.Player.Mind.Soul);
        }

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

        private void GiveRoleLoadoutToPlayer(Soul soul)
        {
            foreach (var rolePlayer in rolePlayers)
            {
                Punpun.Say(this, rolePlayer.Key.Ckey + " role is " + rolePlayer.Value.Name);
            }
        }
    }
}