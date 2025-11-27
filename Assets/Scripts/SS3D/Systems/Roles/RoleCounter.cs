using SS3D.Systems.Entities;
using System.Collections.Generic;

namespace SS3D.Systems.Roles
{
    /// <summary>
    /// A counter of how many players there are in a Role and how many slots are left
    /// </summary>
    public class RoleCounter
    {
        private readonly List<Player> _players = new();

        public RoleCounter(RoleData role, int availableRoles)
        {
            Role = role;
            AvailableRoles = availableRoles;
        }

        public RoleData Role { get; private set; }

        public int AvailableRoles { get; private set; }

        public int CurrentRoles => _players.Count;

        /// <summary>
        /// Add player to role if there are available slots
        /// </summary>
        /// <param name="player"></param>
        public void AddPlayer(Player player)
        {
            if (CurrentRoles < AvailableRoles || AvailableRoles == 0)
            {
                _players.Add(player);
            }
        }

        /// <summary>
        /// Remove player from role
        /// </summary>
        /// <param name="player"></param>
        public void RemovePlayer(Player player)
        {
            _players.Remove(player);
        }
    }
}
