using FishNet.Connection;
using FishNet.Object;
using SS3D.Permissions;
using SS3D.Systems.Health;
using UnityEngine;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    public class DestroyBodyPartCommand : Command
    {
        public override string LongDescription => "Force-sever a player's head (decapitation / mind-swap test)";
        public override string ShortDescription => "Sever head";
        public override string Usage => "(game object name)";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
        public override CommandType Type => CommandType.Server;

        private record CalculatedValues(HumanHealthController Health) : ICalculatedValues;

        [Server]
        public override string Perform(string[] args, NetworkConnection conn = null)
        {
            if (!ReceiveCheckResponse(args, out CheckArgsResponse response, out CalculatedValues values))
            {
                return response.InvalidArgs;
            }

            values.Health.ApplyDamage(BodyZone.Head, HealthConstants.DisabledThreshold, 0f);
            if (!values.Health.TrySeverZone(BodyZone.Head, force: true))
            {
                return "Head severance failed";
            }

            return "Head severed";
        }

        [Server]
        protected override CheckArgsResponse CheckArgs(string[] args)
        {
            CheckArgsResponse response = new();

            if (args.Length != 1)
            {
                return response.MakeInvalid("Invalid number of arguments");
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

            return response.MakeValid(new CalculatedValues(health));
        }
    }
}
