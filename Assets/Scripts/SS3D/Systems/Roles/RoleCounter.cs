using SS3D.Systems.Entities;
using System.Collections.Generic;

namespace SS3D.Systems.Roles
{
    /// <summary>
    /// A counter of how many players there are in a Role and how many slots are left
    /// </summary>
    public class RoleCounter
    {
        public RoleData role;
        public int currentRoles;
        public int availableRoles;
        public List<Soul> players = new List<Soul>();

        /// <summary>
        /// Add player to role if there are available slots
        /// </summary>
        /// <param name="player"></param>
        public void AddPlayer(Soul player)
        {
            if (currentRoles < availableRoles || availableRoles == 0)
            {
                players.Add(player);
                currentRoles++;
            }
        }

        /// <summary>
        /// Remove player from role
        /// </summary>
        /// <param name="player"></param>
        public void RemovePlayer(Soul player)
        {
            players.Remove(player);
            currentRoles--;
        }
    }
}