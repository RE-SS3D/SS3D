using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SS3D.Core;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Permissions;
using SS3D.Systems.PlayerControl;
using UnityEngine.Device;

namespace SS3D.Systems.IngameConsoleSystem
{
    public static class CommandsContainer
    {
        private const string WrongArgsText = "Wrong args. Type \"(command) help\"";
        [ShortDescription("Repeat your string")]
        [LongDescription("echo (number) (your string)")]
        public static string Echo(string times, params object[] a)
        {
            int timesNumber;
            try
            {
                timesNumber = Int32.Parse(times);
            }
            catch (FormatException)
            {
                return WrongArgsText;
            }
            return String.Concat(Enumerable.Repeat(string.Join(" ", a), timesNumber));
        }
        [ShortDescription("Close app")]
        [LongDescription("Close app")]
        public static string Quit()
        {
            Application.Quit();
            return "Done";
        }
        [ShortDescription("Restart app")]
        [LongDescription("Restart app")]
        public static string Reconnect()
        {
            Process.Start(Application.dataPath.Replace("_Data", ".exe"));
            Application.Quit();
            return "Done";
        }
        [ShortDescription("Show all players online")]
        [LongDescription("Show all players online")]
        public static string PlayerList()
        {
            string ret = "";
            List<Soul> souls = SystemLocator.Get<PlayerSystem>().OnlineSouls.ToList();
            foreach (Soul i in souls)
            {
                ret += i.Ckey + "\t";
            }
            return ret;
        }

        [ShortDescription("Show all entity online")]
        [LongDescription("Show all entity online")]
        public static string EntityList()
        {
            var entities = SystemLocator.Get<EntitySystem>().SpawnedPlayers;
            string ret = "";
            foreach (Entity i in entities)
            {
                ret += i.Ckey + "\t";
            }
            return ret;
        }


        [ShortDescription("Show all souls")]
        [LongDescription("Show all souls")]
        public static string SoulList()
        {
            string ret = "";
            IEnumerable<Soul> souls = SystemLocator.Get<PlayerSystem>().ServerSouls;
            foreach (Soul i in souls)
            {
                ret += i.Ckey + "\t";
            }
            return ret;
        }
        [ShortDescription("Change user permission")]
        [LongDescription("changeperms (user ckey) (required role)")]
        public static string ChangePerms(string ckey, string role)
        {
            string[] roleNames = typeof(ServerRoleTypes).GetFields().Select(item => item.Name).ToArray();
            string foundRoleName = Array.Find(roleNames, item => item.ToLower() == role);
            if (foundRoleName != null)
            {
                ServerRoleTypes.TryParse(foundRoleName, out ServerRoleTypes foundRole);
                SystemLocator.Get<PermissionSystem>().ChangeUserPermission(ckey, foundRole);
                return "Done";
            }
            else
            {
                return "This role doesn't exist";
            }
        }

        [ShortDescription("inspect body part")]
        [LongDescription("inspect body part")]
        public static string LogBodyPart(string ckey, string bodyPartName)
        {
            Soul player = SystemLocator.Get<PlayerSystem>().GetSoul(ckey);
            if (player == null)
                return "This player doesn't exist";

            var entity = SystemLocator.Get<EntitySystem>().GetSpawnedEntity(player);
            if (entity == null)
                return "This player doesn't exist";

            var bodyParts = entity.Root.gameObject.GetComponentsInChildren<BodyPartBehaviour>();
            var bodyPartsWithName = bodyParts.Where(x => x.gameObject.name == bodyPartName).ToList();

            string description = "";

            if (bodyPartsWithName.Count == 0)
            {
                return "No body parts with this name on player " + ckey;
            }

            foreach (var bodyPart in bodyPartsWithName)
            {
                description = bodyPart.DescribeBodyPart();
            }

            return description;
        }

        [ShortDescription("inspect body part")]
        [LongDescription("inspect body part")]
        public static string AddNerveLayer(string ckey, string bodyPartName)
        {
            Soul player = SystemLocator.Get<PlayerSystem>().GetSoul(ckey);
            if (player == null)
                return "This player doesn't exist";

            var entity = SystemLocator.Get<EntitySystem>().GetSpawnedEntity(player);
            if (entity == null)
                return "This player doesn't exist";

            var bodyParts = entity.Root.gameObject.GetComponentsInChildren<BodyPartBehaviour>();
            var bodyPartsWithName = bodyParts.Where(x => x.gameObject.name == bodyPartName).ToList();

            string description = "";

            if (bodyPartsWithName.Count == 0)
            {
                return "No body parts with this name on player " + ckey;
            }

            foreach (var bodyPart in bodyPartsWithName)
            {
                bodyPart.AddBodyLayer(new NerveLayer(bodyPart.BodyPart));
            }

            return description;
        }

        [ShortDescription("inspect body part")]
        [LongDescription("inspect body part")]
        public static string AddConnectedBodyPart(string ckey, string bodyPartName)
        {
            Soul player = SystemLocator.Get<PlayerSystem>().GetSoul(ckey);
            if (player == null)
                return "This player doesn't exist";

            var entity = SystemLocator.Get<EntitySystem>().GetSpawnedEntity(player);
            if (entity == null)
                return "This player doesn't exist";

            var bodyParts = entity.Root.gameObject.GetComponentsInChildren<BodyPartBehaviour>();
            var bodyPartsWithName = bodyParts.Where(x => x.gameObject.name == bodyPartName).ToList();

            string description = "";

            if (bodyPartsWithName.Count == 0)
            {
                return "No body parts with this name on player " + ckey;
            }

            foreach (var bodyPart in bodyPartsWithName)
            {
                bodyPart.AddBodyLayer(new NerveLayer(bodyPart.BodyPart));
            }

            return description;
        }


    }
}