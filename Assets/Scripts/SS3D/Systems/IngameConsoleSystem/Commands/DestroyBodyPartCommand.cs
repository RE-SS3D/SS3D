using FishNet.Connection;
using FishNet.Object;
using SS3D.Permissions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    public class DestroyBodyPartCommand : Command
    {
        public override string LongDescription => "Destroy a given body part, unattached from a player. \n " +
            "Usage : destroybodypart [game object name]";
        public override string ShortDescription => "Hit me daddy";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
        public override CommandType Type => CommandType.Server;

        private record CalculatedValues(IEnumerable<BodyPart> BodyParts) : ICalculatedValues;

        [Server]
        public override string Perform(string[] args, NetworkConnection conn = null)
        {
            if (!ReceiveCheckResponse(args, out CheckArgsResponse response, out CalculatedValues values)) return response.InvalidArgs;

            values.BodyParts.First().InflictDamageToAllLayer(new (Health.DamageType.Heat, 10000000000));
            return "BodyPart hurt";
        }

        [Server]
        protected override CheckArgsResponse CheckArgs(string[] args)
        {
            CheckArgsResponse response = new();

            if (args.Length != 1) return response.MakeInvalid("Invalid number of arguments");
            
            string gameObjectName = args[0];
            GameObject go = GameObject.Find(gameObjectName);
            if (go == null) return response.MakeInvalid("No bodypart with this name");

            BodyPart[] bodyParts = go.GetComponentsInChildren<BodyPart>().Where(x => x.gameObject.name == gameObjectName).ToArray();
            if (!bodyParts.Any()) return response.MakeInvalid("No bodypart with this name");

            if (bodyParts.Length != 1) return response.MakeInvalid("Multiple body parts with the same name, ambiguous command");

            return response.MakeValid(new CalculatedValues(bodyParts));
        }
    }
}


