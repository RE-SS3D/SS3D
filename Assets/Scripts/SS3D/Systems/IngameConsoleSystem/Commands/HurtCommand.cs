using FishNet.Connection;
using FishNet.Object;
using SS3D.Core;
using SS3D.Permissions;
using SS3D.Systems.Entities;
using SS3D.Systems.PlayerControl;
using System;
using System.Collections.Generic;
using System.Linq;
using SS3D.Systems.Health;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    public class HurtCommand : Command
    {
        public override string LongDescription => "Hurt body layer in a body part of a player";
        public override string Usage => "(ckey) (body part name) (body layer name) (damage type) (damage amount)\n"
            + "example: hurt editorUser HumanHead all toxic 10";
        public override string ShortDescription => "Hurt me daddy";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
        public override CommandType Type => CommandType.Server;

        private record CalculatedValues(BodyPart BodyPart, BodyLayerType BodyLayerType, DamageType DamageType, int DamageAmount) : ICalculatedValues;

        [Server]
        public override string Perform(string[] args, NetworkConnection conn = null)
        {
            if (!ReceiveCheckResponse(args, out CheckArgsResponse response, out CalculatedValues values)) return response.InvalidArgs;
            string bodyLayerName = args[2];

			if (bodyLayerName == "all")
			{
				values.BodyPart.InflictDamageToAllLayer(new (values.DamageType, values.DamageAmount));
			}
			else if (!values.BodyPart.TryInflictDamage(values.BodyLayerType, new(values.DamageType, values.DamageAmount)))
            {
                return response.MakeInvalid("can't inflict damage on bodypart").InvalidArgs;
            }
            return "Player hurt";
        }

        [Server]
        protected override CheckArgsResponse CheckArgs(string[] args)
        {

            CheckArgsResponse response = new();

            if (args.Length != 5) return response.MakeInvalid("Invalid number of arguments");

            string ckey = args[0];
            string bodyPartName = args[1];
            string bodyLayerName = args[2];
            string damageTypeName = args[3];
            string damageAmountString = args[4];
            int damageAmount = 0;
            BodyLayerType bodyLayerType;
            DamageType damageType;
            
            if (!int.TryParse(damageAmountString, out damageAmount)) return response.MakeInvalid("Invalid damage amount");

            if (!Enum.TryParse(bodyLayerName, true, out bodyLayerType) && bodyLayerName != "all") 
                return response.MakeInvalid("Invalid body layer type");

            if (!Enum.TryParse(damageTypeName, true, out damageType)) return response.MakeInvalid("Invalid damage type");

            Player player = SubSystems.Get<PlayerSubSystem>().GetPlayer(ckey);
            if (player == null) return response.MakeInvalid("This player doesn't exist");

            Entity entity = SubSystems.Get<EntitySubSystem>().GetSpawnedEntity(player);
            if (entity == null) return response.MakeInvalid("This entity doesn't exist");
            
            IEnumerable<BodyPart> bodyParts = entity.GetComponentsInChildren<BodyPart>().Where(x => x.gameObject.name == bodyPartName).ToArray();

            if (!bodyParts.Any()) return response.MakeInvalid("No bodypart with this name");

            if (bodyParts.Count() != 1) return response.MakeInvalid("Multiple body parts with the same name, ambiguous command");
            
            BodyPart bodyPart = bodyParts.First();
            if (bodyLayerName != "all" && !bodyPart.ContainsLayer(bodyLayerType)) return response.MakeInvalid("body layer not present on the bodypart");
            
            return response.MakeValid(new CalculatedValues(bodyPart, bodyLayerType, damageType, damageAmount));
        }
    }
}
