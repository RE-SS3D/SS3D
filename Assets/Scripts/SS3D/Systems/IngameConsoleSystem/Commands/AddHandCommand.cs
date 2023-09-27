using SS3D.Core;
using SS3D.Systems.Entities;
using SS3D.Systems.PlayerControl;
using UnityEngine;
using FishNet;
using SS3D.Data;
using SS3D.Data.Enums;
using SS3D.Systems.Inventory.Containers;
using FishNet.Connection;
using SS3D.Permissions;
using System;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    /// <summary>
    /// Command to add a hand to an entity.
    /// This is mostly used for testing purpose, to check if hands can correctly be added to an entity and if they behave
    /// as expected.
    /// </summary>
    public class AddHandCommand : Command
    {
        public override string ShortDescription => "Add hand to user";
        public override string Usage => "(ckey) [(position) (rotation)] \nPosition and rotation are float arrays and written as x y z";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
        public override CommandType Type => CommandType.Server;
        
        private record CalculatedValues(Player Player, Entity Entity, Vector3 Position, Vector3 Rotation) : ICalculatedValues;
        
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

            Player Player = Subsystems.Get<PlayerSystem>().GetPlayer(ckey);
            Entity entity = Subsystems.Get<EntitySystem>().GetSpawnedEntity(Player);

            GameObject leftHandPrefab = Bodyparts.Get<GameObject>(BodyPartsIds.HumanHandLeft);
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
            CheckArgsResponse response = new();
            if (args.Length != 1 && args.Length != 7) return response.MakeInvalid("Invalid number of arguments");
            
            string ckey = args[0];
            Player player = Subsystems.Get<PlayerSystem>().GetPlayer(ckey);
            if (player == null) return response.MakeInvalid("This player doesn't exist");

            Entity entity = Subsystems.Get<EntitySystem>().GetSpawnedEntity(player);
            if (entity == null) return response.MakeInvalid("This entity doesn't exist");
            
            Vector3 position;
            Vector3 rotation;
            if (args.Length > 1)
            {
                try
                {
                    position = new(float.Parse(args[1]), float.Parse(args[2]), float.Parse(args[3]));
                    rotation = new(float.Parse(args[4]), float.Parse(args[5]), float.Parse(args[6]));
                }
                catch (FormatException)
                {
                    return response.MakeInvalid("Incorrect position/rotation format");
                }
            }
            else
            {
                position = new(0.5f, 0.7f, 0);
                rotation = new(-50, -270, 90);
            }
            
            return response.MakeValid(new CalculatedValues(player, entity, position, rotation));
        }
    }
}
