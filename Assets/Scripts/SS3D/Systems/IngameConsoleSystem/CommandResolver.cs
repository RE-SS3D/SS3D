using System;
using SS3D.Core;
using SS3D.Data;
using SS3D.Data.AssetDatabases;
using SS3D.Data.Generated;
using SS3D.Systems.Entities;
using SS3D.Systems.Health;
using SS3D.Systems.IngameConsoleSystem.Commands;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.PlayerControl;
using UnityEngine;

namespace SS3D.Systems.IngameConsoleSystem
{
    /// <summary>
    /// The live <see cref="ICommandResolver"/>: <c>me</c> is the caller's body or ghost, a ckey is that player's
    /// body or ghost (exact, case-sensitive match, as <see cref="PlayerSubSystem.GetPlayer(string)"/>), and
    /// <c>nearest</c> is the closest other body with health within <see cref="NearestRange"/>, so mindless bodies
    /// such as combat dummies can be targeted. Server commands resolve on the server.
    /// </summary>
    public sealed class CommandResolver : ICommandResolver
    {
        public const string MeToken = "me";

        public const string NearestToken = "nearest";

        /// <summary>Metres. Keeps <c>kill nearest</c> from reaching a player across the station.</summary>
        public const float NearestRange = 5f;

        public bool TryResolveTarget(string token, CommandContext context, out CommandTarget target, out string error)
        {
            target = null;
            error = null;

            if (string.Equals(token, MeToken, StringComparison.OrdinalIgnoreCase))
            {
                if (context?.CallerEntity == null)
                {
                    error = "you have no body or ghost to target";
                    return false;
                }

                target = FromEntity(token, context.CallerEntity);
                return true;
            }

            if (string.Equals(token, NearestToken, StringComparison.OrdinalIgnoreCase))
            {
                return TryResolveNearest(token, context, out target, out error);
            }

            if (!SubSystems.TryGet(out PlayerSubSystem players) || !SubSystems.TryGet(out EntitySubSystem entities))
            {
                error = "no players are loaded";
                return false;
            }

            Player player = players.GetPlayer(token);
            if (player == null)
            {
                error = $"no player with ckey '{token}' (ckeys are case-sensitive; or use me / nearest)";
                return false;
            }

            Entity entity = entities.GetSpawnedEntity(player);
            if (entity == null)
            {
                error = $"player '{token}' has no body or ghost";
                return false;
            }

            target = new CommandTarget(token, player.Ckey, entity, player);
            return true;
        }

        private static bool TryResolveNearest(string token, CommandContext context, out CommandTarget target, out string error)
        {
            target = null;
            error = null;
            Entity caller = context?.CallerEntity;
            if (caller == null)
            {
                error = "nearest needs you to have a body or ghost";
                return false;
            }

            Vector3 origin = caller.transform.position;
            Entity best = null;
            float bestDistance = NearestRange;
            foreach (HumanHealthController health in UnityEngine.Object.FindObjectsByType<HumanHealthController>(FindObjectsSortMode.None))
            {
                if (health == null || !health.isActiveAndEnabled)
                {
                    continue;
                }

                Entity entity = health.GetComponent<Entity>();
                if (entity == null || entity == caller)
                {
                    continue;
                }

                float distance = Vector3.Distance(origin, entity.transform.position);
                if (distance <= bestDistance)
                {
                    best = entity;
                    bestDistance = distance;
                }
            }

            if (best == null)
            {
                error = $"no body within {NearestRange:0} m";
                return false;
            }

            target = FromEntity(token, best);
            return true;
        }

        private static CommandTarget FromEntity(string token, Entity entity)
        {
            Player player = entity.Mind != null ? entity.Mind.player : null;
            string name = player != null && !string.IsNullOrEmpty(player.Ckey) ? player.Ckey : entity.name;
            return new CommandTarget(token, name, entity, player);
        }

        public bool TryResolveItem(string token, out CommandItem item, out string error)
        {
            if (TryResolveItemAsset(token, out Item prefab, out string assetId))
            {
                item = new CommandItem(token, prefab, assetId);
                error = null;
                return true;
            }

            item = null;
            error = $"no item named '{token}'";
            return false;
        }

        /// <summary>
        /// Resolve by asset id (Items.* constants) or by item or prefab name, case-insensitively.
        /// Assets.Get would log an error for a name, so only TryGet is used.
        /// </summary>
        public static bool TryResolveItemAsset(string itemNameOrId, out Item item, out string assetId)
        {
            if (Assets.TryGet(AssetDatabases.Items, itemNameOrId, out item) && item != null)
            {
                assetId = itemNameOrId;
                return true;
            }

            AssetDatabase database = Assets.GetDatabase(AssetDatabases.Items);
            if (database?.Assets != null)
            {
                foreach (var pair in database.Assets)
                {
                    Item candidate = pair.Value switch
                    {
                        Item direct => direct,
                        GameObject go => go.GetComponent<Item>(),
                        _ => null,
                    };

                    if (candidate == null)
                    {
                        continue;
                    }

                    if (string.Equals(candidate.Name, itemNameOrId, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(candidate.gameObject.name, itemNameOrId, StringComparison.OrdinalIgnoreCase))
                    {
                        item = candidate;
                        assetId = pair.Key;
                        return true;
                    }
                }
            }

            item = null;
            assetId = null;
            return false;
        }
    }
}
