using FishNet.Connection;
using FishNet.Object;
using SS3D.Core;
using SS3D.Systems.Entities;
using SS3D.Systems.Permissions;
using SS3D.Systems.PlayerControl;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
	public class HurtCommand : Command
	{
		public override string LongDescription => "Hurt a given body part and body layer of a player by a given amount of damages from a given type";
		public override string ShortDescription => "Hurt me daddy";
		public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
		public override CommandType Type => CommandType.Server;


		[Server]
		public override string Perform(string[] args, NetworkConnection conn = null)
		{
			CheckArgsResponse checkArgsResponse = CheckArgs(args);
			if (checkArgsResponse.IsValid == false)
				return checkArgsResponse.InvalidArgs;

			string ckey = args[0];
			string bodyPartName = args[1];
			string bodyLayerName = args[2];
			string damageTypeName = args[3];
			string damageAmountString = args[4];

			int damageAmount = 0;
			BodyLayerType bodyLayerType;
			DamageType damageType;

			Player player= Subsystems.Get<PlayerSystem>().GetPlayer(ckey);
			Entity entity = Subsystems.Get<EntitySystem>().GetSpawnedEntity(player);

			IEnumerable<BodyPart> bodyParts = entity.GetComponentsInChildren<BodyPart>().Where(x => x.gameObject.name == bodyPartName);
			BodyPart bodyPart = bodyParts.First();

			Enum.TryParse(bodyLayerName, true, out bodyLayerType);
			Enum.TryParse(damageTypeName, true, out damageType);
			damageAmount = int.Parse(damageAmountString);

			BodyLayer bodyLayer = bodyPart.FirstBodyLayerOfType(bodyLayerType);

			if(!bodyPart.TryInflictDamage(bodyLayerType, new DamageTypeQuantity(damageType, damageAmount)))
			{
				checkArgsResponse.IsValid= false;
				return checkArgsResponse.InvalidArgs = "can't inflict damage on bodypart";
			}
			return "Player hurt";
		}

		[Server]
		protected override CheckArgsResponse CheckArgs(string[] args)
		{

			CheckArgsResponse response = new CheckArgsResponse();

			if (args.Length != 5)
			{
				response.IsValid = false;
				response.InvalidArgs = "Invalid number of arguments";
				return response;
			}

			string ckey = args[0];
			string bodyPartName = args[1];
			string bodyLayerName = args[2];
			string damageTypeName = args[3];
			string damageAmountString = args[4];
			int damageAmount = 0;
			BodyLayerType bodyLayerType;
			DamageType damageType;

			try
			{
				damageAmount = int.Parse(damageAmountString);
			}
			catch (Exception)
			{
				response.IsValid = false;
				response.InvalidArgs = "Something went wrong with the damage amount conversion to integer, provide a valid number";
				return response;
			}


			if (!Enum.TryParse(bodyLayerName, true, out bodyLayerType))
			{
				response.IsValid = false;
				response.InvalidArgs = "Provide a valid body layer type name";
				return response;
			}

			if (!Enum.TryParse(damageTypeName, true, out damageType))
			{
				response.IsValid = false;
				response.InvalidArgs = "Provide a valid damage type name";
				return response;
			}


			Player player = Subsystems.Get<PlayerSystem>().GetPlayer(ckey);
			if (player == null)
			{
				response.IsValid = false;
				response.InvalidArgs = "This player doesn't exist";
				return response;
			}

			Entity entity = Subsystems.Get<EntitySystem>().GetSpawnedEntity(player);
			if (entity == null)
			{
				response.IsValid = false;
				response.InvalidArgs = "This entity doesn't exist";
				return response;
			}

			IEnumerable<BodyPart> bodyParts = entity.GetComponentsInChildren<BodyPart>().Where(x => x.gameObject.name == bodyPartName);

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

			BodyPart bodyPart = bodyParts.First();

			if (!bodyPart.ContainsLayer(bodyLayerType))
			{
				response.IsValid = false;
				response.InvalidArgs = "body layer not present on the bodypart";
				return response;
			}

			response.IsValid = true;
			return response;
		}
	}
}
