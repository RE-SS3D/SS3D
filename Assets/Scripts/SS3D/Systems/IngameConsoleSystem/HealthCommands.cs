using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SS3D.Core;
using SS3D.Systems.Entities;
using SS3D.Systems.Permissions;
using SS3D.Systems.PlayerControl;
using UnityEngine.Device;


namespace SS3D.Systems.IngameConsoleSystem
{
    public static class HealthCommands
    {
        public static string InspectBodypart(string ckey, string bodyPartName)
        {
            Soul Player = SystemLocator.Get<PlayerSystem>().GetSoul(ckey);
            var connection = Player.NetworkObject.Owner;
            var bodyParts = Player.gameObject.GetComponentsInChildren<BodyPartBehaviour>();
            var bodyPartsWithName = bodyParts.Where(x => x.gameObject.name == bodyPartName).ToList();

            string description = "";

            if(bodyPartsWithName.Count == 0)
            {
                return "No body parts with this name on player " + ckey; 
            }

            foreach ( var bodyPart in bodyPartsWithName)
            {
                description = bodyPart.DescribeBodyPart();
            }

            return description;
        }
    }
}
