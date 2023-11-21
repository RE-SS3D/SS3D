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
        public override string LongDescription => "add (ckey) [(position) (rotation)]\n Position and rotation are float arrays and written as x y z";
        public override string ShortDescription => "add hand to user";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;

        public override CommandType Type => CommandType.Server;
        private struct CalculatedValues : ICalculatedValues
        {
            public Player Player;
            public Entity Entity;
            public Vector3 Position;
            public Vector3 Rotation;
        }
        public override string Perform(string[] args, NetworkConnection conn)
        {
            if (!ReceiveCheckResponse(args, out CheckArgsResponse response, out CalculatedValues calculatedValues)) return response.InvalidArgs;
            
            GameObject leftHandPrefab = Assets.Get<GameObject>((int)AssetDatabases.BodyParts, (int)BodyPartsIds.HumanHandLeft);
            GameObject leftHandObject = GameObject.Instantiate(leftHandPrefab, calculatedValues.Entity.transform);
            leftHandObject.transform.localPosition = calculatedValues.Position;
            leftHandObject.transform.localEulerAngles = calculatedValues.Rotation;
            InstanceFinder.ServerManager.Spawn(leftHandObject, calculatedValues.Player.Owner);
            calculatedValues.Entity.GetComponent<HumanInventory>().TryAddContainer(leftHandObject.GetComponent<AttachedContainer>());
            calculatedValues.Entity.GetComponent<Hands>().AddHand(leftHandObject.GetComponent<Hand>());

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
            
            return response.MakeValid(new CalculatedValues{Player = player, Entity = entity, Position = position, Rotation = rotation});
        }
    }
}
