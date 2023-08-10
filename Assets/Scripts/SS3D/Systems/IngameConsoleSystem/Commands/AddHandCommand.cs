using SS3D.Core;
using SS3D.Systems.Entities;
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
using FishNet.Connection;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
	/// <summary>
	/// Command to add a hand to an entity.
	/// This is mostly used for testing purpose, to check if hands can correctly be added to an entity and if they behave
	/// as expected.
	/// </summary>
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
			CmdPerform(args);

			return "hand added";
		}
		protected override CheckArgsResponse CheckArgs(string[] args)
		{
			CheckArgsResponse response = new CheckArgsResponse();
			if (args.Length != 1 && args.Length != 7)
			{
				response.IsValid = false;
				response.InvalidArgs = "Invalid number of arguments";
				return response;
			}
			string ckey = args[0];
			Player player = Subsystems.Get<PlayerSystem>().GetPlayer(ckey);
			if (player == null)
			{
				response.IsValid = false;
				response.InvalidArgs = "This player doesn't exist";
				return response;
			}
			Entity entityToKill = Subsystems.Get<EntitySystem>().GetSpawnedEntity(player);
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
		private void CmdPerform(string[] args, NetworkConnection conn = null)
		{
			string ckey = args[0];
			if(!Subsystems.Get<PermissionSystem>().TryGetUserRole(args[0], out ServerRoleTypes userPermission))
			{
				return;
			}

			if(userPermission < AccessLevel)
			{
				return;
			}

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
