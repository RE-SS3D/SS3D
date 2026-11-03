using System.Collections.Generic;
using FishNet.Object;
using SS3D.Core;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Health;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Inventory.Items.Generic;
using SS3D.Systems.Roles;
using UnityEngine;

namespace SS3D.Systems.Gamemodes
{
    /// <summary>
    /// Server: reads the world into a <see cref="NukeOpsSnapshot"/> for <see cref="NukeOpsRules"/>. An operative's
    /// entity is found through its mind, so a death (body swapped for a ghost) reads as dead and an SSD body
    /// still reads as up. The disk's carrier is found by walking up its container tree.
    /// </summary>
    public static class NukeOpsWorld
    {
        [Server]
        public static NukeOpsSnapshot Read()
        {
            if (!SubSystems.TryGet(out RoleSubSystem roles) || !SubSystems.TryGet(out EntitySubSystem entities))
                return new NukeOpsSnapshot(null, null);

            List<OperativeBody> squad = new();
            Dictionary<Entity, string> livingOperatives = new();
            foreach (Player player in roles.Operatives)
            {
                if (player == null)
                    continue;

                Entity entity = FindEntity(entities, player);
                bool connected = player.Owner != null && player.Owner.IsActive;
                if (entity == null)
                {
                    squad.Add(new OperativeBody(player.Ckey, hasBody: false, dead: false, connected));
                    continue;
                }

                bool dead = IsDead(entity);
                squad.Add(new OperativeBody(player.Ckey, hasBody: true, dead, connected));
                if (!dead)
                    livingOperatives[entity] = player.Ckey;
            }

            return new NukeOpsSnapshot(squad, FindDiskCarrier(livingOperatives));
        }

        /// <summary>Null-safe: during a death the new ghost is in the list before it holds the mind.</summary>
        private static Entity FindEntity(EntitySubSystem entities, Player player)
        {
            foreach (Entity entity in entities.SpawnedPlayers)
            {
                if (entity != null && entity.Mind != null && entity.Mind.player == player)
                    return entity;
            }

            return null;
        }

        private static bool IsDead(Entity entity) =>
            entity.GetComponent<HumanoidGhostController>() != null
            || (entity.TryGetComponent(out HumanHealthController health) && health.Snapshot.State == HealthState.Dead);

        /// <summary>The ckey of the living operative holding any disk, else null.</summary>
        private static string FindDiskCarrier(Dictionary<Entity, string> livingOperatives)
        {
            if (livingOperatives.Count == 0)
                return null;

            foreach (NukeCard disk in Object.FindObjectsByType<NukeCard>(FindObjectsSortMode.None))
            {
                string ckey = NukeOpsRules.CarrierCkey<Component>(disk, HolderOf,
                    node => node is Entity entity && livingOperatives.TryGetValue(entity, out string carrier) ? carrier : null);
                if (ckey != null)
                    return ckey;
            }

            return null;
        }

        /// <summary>
        /// What holds a node: an item's container belongs to another item (a bag) or to a body. A body holds
        /// nothing further up.
        /// </summary>
        private static Component HolderOf(Component node)
        {
            if (node is not Item item || item.Container == null)
                return null;

            Item bag = item.Container.GetComponentInParent<Item>();
            if (bag != null && bag != item)
                return bag;

            return item.Container.GetComponentInParent<Entity>();
        }
    }
}
