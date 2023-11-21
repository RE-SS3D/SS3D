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
        public override string LongDescription => "Hurt a given body part and body layer of a player by a given amount of damages from a given type";
        public override string ShortDescription => "Hurt me daddy";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
        public override CommandType Type => CommandType.Server;
        
        private struct CalculatedValues : ICalculatedValues
        {
            public BodyPart BodyPart;
            public BodyLayerType BodyLayerType;
            public DamageType DamageType;
            public int DamageAmount;
        }
        
        [Server]
        public override string Perform(string[] args, NetworkConnection conn = null)
        {
            if (!ReceiveCheckResponse(args, out CheckArgsResponse response, out CalculatedValues calculatedValues)) return response.InvalidArgs;
            string bodyLayerName = args[2];

			if (bodyLayerName == "all")
			{
				calculatedValues.BodyPart.InflictDamageToAllLayer(new (calculatedValues.DamageType, calculatedValues.DamageAmount));
			}
			else if (!calculatedValues.BodyPart.TryInflictDamage(calculatedValues.BodyLayerType, new(calculatedValues.DamageType, calculatedValues.DamageAmount)))
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

            Player player = Subsystems.Get<PlayerSystem>().GetPlayer(ckey);
            if (player == null) return response.MakeInvalid("This player doesn't exist");

            Entity entity = Subsystems.Get<EntitySystem>().GetSpawnedEntity(player);
            if (entity == null) return response.MakeInvalid("This entity doesn't exist");
            
            IEnumerable<BodyPart> bodyParts = entity.GetComponentsInChildren<BodyPart>().Where(x => x.gameObject.name == bodyPartName).ToArray();

            if (!bodyParts.Any()) return response.MakeInvalid("No bodypart with this name");

            if (bodyParts.Count() != 1) return response.MakeInvalid("Multiple body parts with the same name, ambiguous command");
            
            BodyPart bodyPart = bodyParts.First();
            if (bodyLayerName != "all" && !bodyPart.ContainsLayer(bodyLayerType)) return response.MakeInvalid("body layer not present on the bodypart");
            
            return response.MakeValid(new CalculatedValues{BodyPart = bodyPart, BodyLayerType = bodyLayerType, 
                DamageType = damageType, DamageAmount = damageAmount});
        }
    }
}
