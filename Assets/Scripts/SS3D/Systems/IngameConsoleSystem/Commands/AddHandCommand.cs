using System;
using SS3D.Core;
using SS3D.Systems.Entities;
using SS3D.Systems.PlayerControl;
using UnityEngine;
using SS3D.Systems.Inventory.Containers;
using FishNet.Connection;
using SS3D.Data;
using SS3D.Logging;
using SS3D.Permissions;
using SS3D.Data.Generated;
using SS3D.Data.Networking;

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

            if (!SubSystems.Get<AssetSubSystem>().Has(Items.HumanHandLeft))
            {
                return "Hand asset not found";
            }

            Player player = SubSystems.Get<PlayerSubSystem>().GetPlayer(ckey);
            Entity entity = SubSystems.Get<EntitySubSystem>().GetSpawnedEntity(player);

            AddHandAsync(entity, position, rotation, player);

            return "Adding hand...";
        }

        private async void AddHandAsync(Entity entity, Vector3 position, Vector3 rotation, Player player)
        {
            try
            {
                AssetHandle<Hand> leftHandHandle = await new AssetRequest<Hand>(Items.HumanHandLeft).LoadAsync();

                if (!leftHandHandle)
                {
                    Log.Error(this, "Failed to load hand asset");
                    return;
                }

                Hand leftHand = UnityEngine.Object.Instantiate(leftHandHandle.Asset, entity.transform);
                leftHand.Transform.localPosition = position;
                leftHand.Transform.localEulerAngles = rotation;

                await NetworkSpawner.SpawnAsync(leftHand, Items.HumanHandLeft, player.Owner);

                Hands hands = entity.GetComponent<Hands>();
                HumanInventory inventory = entity.GetComponent<HumanInventory>();
                inventory.TryAddContainer(leftHand.GetComponent<AttachedContainer>());
                hands.AddHand(leftHand);
            }
            catch (Exception e)
            {
                Log.Error(this, $"Failed to add hand: {e.Message}");
            }
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
