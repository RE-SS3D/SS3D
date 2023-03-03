using SS3D.Systems.Entities;
using System.Collections.Generic;

namespace SS3D.Systems.Roles
{
    public class RoleCounter
    {
        public RoleData role;
        public int currentRoles;
        public int availableRoles;
        public List<Soul> players = new List<Soul>();

        public void AddPlayer(Soul player)
        {
            if (currentRoles < availableRoles || availableRoles == 0)
            {
                players.Add(player);
                currentRoles++;
            }
        }

        public void RemovePlayer(Soul player)
        {
            players.Remove(player);
            currentRoles--;
        }
    }
}