using System.Collections.Generic;
using FishNet;
using FishNet.Object;
using UnityEngine;

namespace SS3D.Systems.Entities
{
    /// <summary>
    /// Authoring invincibility (map editor setting): no health damage and no space float or fall,
    /// so a builder can walk across open space while testing a map.
    /// <para>
    /// Keyed by connection, not entity, so it survives respawns. The server holds the authoritative
    /// set (damage, environment, server-side movement); the owning client mirrors its own flag for
    /// predicted / owner-driven movement. No SyncVar: observers only see the replicated transform.
    /// </para>
    /// </summary>
    public static class EntityInvincibility
    {
        private static readonly HashSet<int> ServerClientIds = new();

        /// <summary>Owner-client mirror of this connection's flag.</summary>
        public static bool LocalPlayerInvincible { get; set; }

        public static void SetServer(int clientId, bool invincible)
        {
            if (invincible)
                ServerClientIds.Add(clientId);
            else
                ServerClientIds.Remove(clientId);
        }

        public static void ClearServer(int clientId) => ServerClientIds.Remove(clientId);

        /// <summary>Cheap early-out for per-tick callers.</summary>
        public static bool AnyActive => LocalPlayerInvincible || ServerClientIds.Count > 0;

        public static bool IsInvincible(Component component)
        {
            if (component == null || !AnyActive)
                return false;

            NetworkObject networkObject = component.GetComponentInParent<NetworkObject>();
            if (networkObject == null || !networkObject.Owner.IsValid)
                return false;

            if (InstanceFinder.IsServer && ServerClientIds.Contains(networkObject.Owner.ClientId))
                return true;

            return networkObject.IsOwner && LocalPlayerInvincible;
        }
    }
}
