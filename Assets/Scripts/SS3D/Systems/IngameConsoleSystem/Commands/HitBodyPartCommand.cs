using FishNet.Connection;
using FishNet.Object;
using SS3D.Permissions;
using SS3D.Systems.Health;
using System;
using UnityEngine;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    public class HitBodyPartCommand : Command
    {
        public override string LongDescription => "Apply zone damage to a scene object with HumanHealthController";
        public override string ShortDescription => "Apply zone damage to a scene object";
        public override string Usage => "(game object name) (BodyZone) (brute) (burn)";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
        public override CommandType Type => CommandType.Server;

        private record CalculatedValues(HumanHealthController Health, BodyZone Zone, float Brute, float Burn) : ICalculatedValues;

        [Server]
        public override string Perform(string[] args, NetworkConnection conn = null)
        {
            if (!ReceiveCheckResponse(args, out CheckArgsResponse response, out CalculatedValues values))
            {
                return response.InvalidArgs;
            }

            values.Health.ApplyDamage(values.Zone, values.Brute, values.Burn);
            return "Damage applied";
        }

        [Server]
        protected override CheckArgsResponse CheckArgs(string[] args)
        {
            CheckArgsResponse response = new();

            if (args.Length != 4)
            {
                return response.MakeInvalid("Invalid number of arguments");
            }

            if (!Enum.TryParse(args[1], true, out BodyZone zone))
            {
                return response.MakeInvalid("Invalid body zone");
            }

            if (!float.TryParse(args[2], out float brute))
            {
                return response.MakeInvalid("Invalid brute amount");
            }

            if (!float.TryParse(args[3], out float burn))
            {
                return response.MakeInvalid("Invalid burn amount");
            }

            GameObject go = GameObject.Find(args[0]);
            if (go == null)
            {
                return response.MakeInvalid("No object with this name");
            }

            if (!go.TryGetComponent(out HumanHealthController health))
            {
                return response.MakeInvalid("Object has no HumanHealthController");
            }

            return response.MakeValid(new CalculatedValues(health, zone, brute, burn));
        }
    }
}
