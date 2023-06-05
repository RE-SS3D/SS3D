using SS3D.Systems.Entities;
using System.Collections.Generic;

namespace SS3D.Systems.Roles
{
    /// <summary>
    /// A counter of how many players there are in a Role and how many slots are left
    /// </summary>
    public class RoleCounter
    {
        public RoleData Role;
        public int CurrentRoles;
        public int AvailableRoles;
        public List<Player> Players = new List<Player>();

        /// <summary>
        /// Add player to role if there are available slots
        /// </summary>
        /// <param name="player"></param>
        public void AddPlayer(Player player)
        {
            if (CurrentRoles < AvailableRoles || AvailableRoles == 0)
            {
                Players.Add(player);
                CurrentRoles++;
            }
        }

        /// <summary>
        /// Remove player from role
        /// </summary>
        /// <param name="player"></param>
        public void RemovePlayer(Player player)
        {
            Players.Remove(player);
            CurrentRoles--;
        }
    }
}