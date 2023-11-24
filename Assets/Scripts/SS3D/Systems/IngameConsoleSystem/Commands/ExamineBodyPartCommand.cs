using FishNet.Connection;
using FishNet.Object;
using SS3D.Permissions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SS3D.Systems.Health;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    public class ExamineBodyPartCommand : Command
    {
        public override string LongDescription => "Examine a detached body part, returning how damaged it is \n " +
            "Usage : examinebodypart [game object name]";
        public override string ShortDescription => "Examine a detached body part";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
        public override CommandType Type => CommandType.Server;

        private record CalculatedValues(IEnumerable<BodyPart> BodyParts) : ICalculatedValues;

        [Server]
        public override string Perform(string[] args, NetworkConnection conn = null)
        {
            if (!ReceiveCheckResponse(args, out CheckArgsResponse response, out CalculatedValues values)) return response.InvalidArgs;

            string answer = "";
            foreach(BodyLayer layer in values.BodyParts.First().BodyLayers)
            {
                answer += layer + ": ";
                foreach (BodyDamageInfo damage in layer.Damages.DamagesInfo.Values)
                {
                    answer += damage.InjuryType + " " + damage.Quantity;
                }
                answer += "\n";
            }
            return answer;
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