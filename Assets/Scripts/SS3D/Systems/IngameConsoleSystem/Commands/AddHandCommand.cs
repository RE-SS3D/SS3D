using SS3D.Core;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Events;
using SS3D.Systems.Permissions;
using SS3D.Systems.PlayerControl;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet;
using UnityEditor;
using SS3D.Data;
using SS3D.Data.Enums;
using FishNet.Object;
using UnityEngine.InputSystem;
using SS3D.Systems.Inventory.Containers;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
	public class AddHandCommand : Command
	{
		public override string LongDescription => "add hand to user (ckey), you can precise position (x,y,z) and rotation (x,y,z) argument ";
		public override string ShortDescription => "add hand to user";
		public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;

		public GameObject hand;

		public override string Perform(string[] args)
		{
			CheckArgsResponse checkArgsResponse = CheckArgs(args);
			if (checkArgsResponse.IsValid == false)
				return checkArgsResponse.InvalidArgs;
			string ckey = args[0];
			PerformOnServer(args);

			return "hand added";
		}
		protected override CheckArgsResponse CheckArgs(string[] args)
		{
			CheckArgsResponse response = new CheckArgsResponse();
			if (args.Length != 1 || args.Length != 7)
			{
				response.IsValid = false;
				response.InvalidArgs = "Invalid number of arguments";
				return response;
			}
			string ckey = args[0];
			Player PlayerToKill = Subsystems.Get<PlayerSystem>().GetPlayer(ckey);
			if (PlayerToKill == null)
			{
				response.IsValid = false;
				response.InvalidArgs = "This player doesn't exist";
				return response;
			}
			Entity entityToKill = Subsystems.Get<EntitySystem>().GetSpawnedEntity(PlayerToKill);
			if (entityToKill == null)
			{
				response.IsValid = false;
				response.InvalidArgs = "This entity doesn't exist";
				return response;
			}
			response.IsValid = true;
			return response;
		}

		[Server]
		private void PerformOnServer(string[] args)
		{
			string ckey = args[0];

			// default transform for hand.
			Vector3 position = new Vector3(0.5f, 0.7f, 0);
			Vector3 rotation = new Vector3(-50, -270, 90);

			if (args.Length > 1)
			{
				position = new Vector3(float.Parse(args[1]), float.Parse(args[2]), float.Parse(args[3]));
				rotation = new Vector3(float.Parse(args[4]), float.Parse(args[5]), float.Parse(args[6]));
			}

			Player Player = Subsystems.Get<PlayerSystem>().GetPlayer(ckey);
			Entity entity = Subsystems.Get<EntitySystem>().GetSpawnedEntity(Player);

			GameObject leftHandPrefab = Assets.Get<GameObject>((int)AssetDatabases.BodyParts, (int)BodyPartsIds.HumanHandLeft);
			GameObject leftHandObject = GameObject.Instantiate(leftHandPrefab, entity.transform);
			leftHandObject.transform.localPosition = position;
			leftHandObject.transform.localEulerAngles = rotation;

			Hand leftHand = leftHandObject.GetComponent<Hand>();
			InstanceFinder.ServerManager.Spawn(leftHandObject, Player.Owner);

			Hands hands = entity.GetComponent<Hands>();
			HumanInventory inventory = entity.GetComponent<HumanInventory>();
			inventory.TryAddContainer(leftHandObject.GetComponent<AttachedContainer>());
			hands.AddHand(leftHand);
			
		}
	}
}
