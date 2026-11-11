using SS3D.Systems.Entities;
using SS3D.Systems.PlayerControl;
using UnityEngine;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    /// <summary>A resolved <c>me</c> / ckey / <c>nearest</c> target.</summary>
    public sealed class CommandTarget
    {
        public CommandTarget(string token, string displayName, Entity entity, Player player)
        {
            Token = token;
            DisplayName = displayName;
            Entity = entity;
            Player = player;
        }

        /// <summary>What was typed: <c>me</c>, a ckey or <c>nearest</c>.</summary>
        public string Token { get; }

        /// <summary>Ckey when the target has a player, otherwise the entity's name. Used in answers and the admin log.</summary>
        public string DisplayName { get; }

        /// <summary>The body or ghost. Null only in tests with a fake resolver.</summary>
        public Entity Entity { get; }

        /// <summary>The player controlling it; null for a mindless body such as a combat dummy.</summary>
        public Player Player { get; }

        public bool TryGetComponent<T>(out T component) where T : Component
        {
            if (Entity == null)
            {
                component = null;
                return false;
            }

            return Entity.TryGetComponent(out component);
        }

        public override string ToString() => DisplayName;
    }
}
