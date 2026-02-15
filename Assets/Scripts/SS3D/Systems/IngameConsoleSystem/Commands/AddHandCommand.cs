using SS3D.Core;
using SS3D.Systems.Entities;
using SS3D.Systems.PlayerControl;
using UnityEngine;
using FishNet;
using SS3D.Systems.Inventory.Containers;
using FishNet.Connection;
using SS3D.Permissions;
using SS3D.Data.Generated;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    /// <summary>
    /// Command to add a hand to an entity.
    /// This is mostly used for testing purpose, to check if hands can correctly be added to an entity and if they behave
    /// as expected.
    /// </summary>
    public class AddHandCommand : Command
    {
        public override string LongDescription => "add (ckey) [(position) (rotation)]\n Position and rotation are float arrays and written as x y z";
        public override string ShortDescription => "add hand to user";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;

        public override CommandType Type => CommandType.Server;
        public override string Perform(string[] args, NetworkConnection conn)
        {
            CheckArgsResponse checkArgsResponse = CheckArgs(args);
            if (checkArgsResponse.IsValid == false)
                return checkArgsResponse.InvalidArgs;

            string ckey = args[0];

            // default transform for hand.
            Vector3 position = new Vector3(0.5f, 0.7f, 0);
            Vector3 rotation = new Vector3(-50, -270, 90);

            if (args.Length > 1)
            {
                position = new Vector3(float.Parse(args[1]), float.Parse(args[2]), float.Parse(args[3]));
                rotation = new Vector3(float.Parse(args[4]), float.Parse(args[5]), float.Parse(args[6]));
            }

            Player Player = SubSystems.Get<PlayerSubSystem>().GetPlayer(ckey);
            Entity entity = SubSystems.Get<EntitySubSystem>().GetSpawnedEntity(Player);

            GameObject leftHandPrefab = Items.HumanHandLeft;
            GameObject leftHandObject = GameObject.Instantiate(leftHandPrefab, entity.transform);
            leftHandObject.transform.localPosition = position;
            leftHandObject.transform.localEulerAngles = rotation;

            Hand leftHand = leftHandObject.GetComponent<Hand>();
            InstanceFinder.ServerManager.Spawn(leftHandObject, Player.Owner);

            Hands hands = entity.GetComponent<Hands>();
            HumanInventory inventory = entity.GetComponent<HumanInventory>();
            inventory.TryAddContainer(leftHandObject.GetComponent<AttachedContainer>());
            hands.AddHand(leftHand);

            return "hand added";
        }
        protected override CheckArgsResponse CheckArgs(string[] args)
        {
            CheckArgsResponse response = new CheckArgsResponse();
            if (args.Length != 1 && args.Length != 7)
            {
                response.IsValid = false;
                response.InvalidArgs = "Invalid number of arguments";
                return response;
            }
            string ckey = args[0];
            Player player = SubSystems.Get<PlayerSubSystem>().GetPlayer(ckey);
            if (player == null)
            {
                response.IsValid = false;
                response.InvalidArgs = "This player doesn't exist";
                return response;
            }
            Entity entityToKill = SubSystems.Get<EntitySubSystem>().GetSpawnedEntity(player);
            if (entityToKill == null)
            {
                response.IsValid = false;
                response.InvalidArgs = "This entity doesn't exist";
                return response;
            }
            response.IsValid = true;
            return response;
        }
    }
}
