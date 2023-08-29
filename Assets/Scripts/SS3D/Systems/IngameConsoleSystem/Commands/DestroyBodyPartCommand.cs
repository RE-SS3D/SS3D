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
	public class DestroyBodyPartCommand : Command
	{
		public override string LongDescription => "Destroy a given body part, unattached from a player. \n " +
			"Usage : destroybodypart [game object name]";
		public override string ShortDescription => "Hit me daddy";
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

			bodyPart.DestroyBodyPart();
			return "BodyPart hurt";
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


