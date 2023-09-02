using FishNet.Connection;
using FishNet.Object;
using SS3D.Systems.Permissions;
using System.Collections;
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


        [Server]
        public override string Perform(string[] args, NetworkConnection conn = null)
        {
            CheckArgsResponse checkArgsResponse = CheckArgs(args);
            if (checkArgsResponse.IsValid == false)
                return checkArgsResponse.InvalidArgs;

            string gameObjectName = args[0];

            GameObject go = GameObject.Find(gameObjectName);
            IEnumerable<BodyPart> bodyParts = go.GetComponentsInChildren<BodyPart>().Where(x => x.gameObject.name == gameObjectName);
            BodyPart bodyPart = bodyParts.First();

            string answer = "";

            foreach(BodyLayer layer in bodyPart.BodyLayers)
            {
                answer += layer.ToString() + ": ";
                foreach (DamageTypeQuantity damageTypeQuantity in layer.DamageTypeQuantities)
                {
                    answer += damageTypeQuantity.damageType.ToString() + " " + damageTypeQuantity.quantity.ToString();
                }
                answer += "\n";
            }
            return answer;
        }

        [Server]
        protected override CheckArgsResponse CheckArgs(string[] args)
        {

            CheckArgsResponse response = new CheckArgsResponse();

            if (args.Length != 1)
            {
                response.IsValid = false;
                response.InvalidArgs = "Invalid number of arguments";
                return response;
            }

            string gameObjectName = args[0];

            GameObject go = GameObject.Find(gameObjectName);
            if (go == null)
            {
                response.IsValid = false;
                response.InvalidArgs = "No bodypart with this name";
                return response;
            }

            IEnumerable<BodyPart> bodyParts = go.GetComponentsInChildren<BodyPart>().Where(x => x.gameObject.name == gameObjectName);

            if (bodyParts.Count() == 0)
            {
                response.IsValid = false;
                response.InvalidArgs = "No bodypart with this name";
                return response;
            }

            if (bodyParts.Count() != 1)
            {
                response.IsValid = false;
                response.InvalidArgs = "Multiple body parts with the same name, ambiguous command";
                return response;
            }

            response.IsValid = true;
            return response;
        }
    }
}