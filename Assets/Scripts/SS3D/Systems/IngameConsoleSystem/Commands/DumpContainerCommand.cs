using FishNet.Connection;
using SS3D.Permissions;
using SS3D.Systems.Inventory.Containers;
using UnityEngine;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
	public class DumpContainerCommand : Command
	{
		public override string LongDescription => "Dump the content of a container. Dump the content of all containers on the game object. \n" +
			"Usage : dumpcontainer [container's game object name]";
		public override string ShortDescription => "Dump the content of a container.";
		public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;

		public override CommandType Type => CommandType.Server;
		public override string Perform(string[] args, NetworkConnection conn)
		{
			CheckArgsResponse checkArgsResponse = CheckArgs(args);
			if (checkArgsResponse.IsValid == false)
				return checkArgsResponse.InvalidArgs;

			string containerName = args[0];
			GameObject containerGo = GameObject.Find(containerName);
			AttachedContainer container = containerGo.GetComponentInChildren<AttachedContainer>();
			container.Dump();
			return "Container content dumped";
		}
		protected override CheckArgsResponse CheckArgs(string[] args)
		{
			CheckArgsResponse response = new CheckArgsResponse();
			if (args.Length != 1)
			{
				response.IsValid = false;
				response.InvalidArgs = "Invalid number of arguments";
				return response;
			}
			string containerName = args[0];
			GameObject containerGo = GameObject.Find(containerName);
			if (containerGo == null)
			{
				response.IsValid = false;
				response.InvalidArgs = "This container doesn't exist";
				return response;
			}
			AttachedContainer[] container =  containerGo.GetComponentsInChildren<AttachedContainer>();
			if (container.Length == 0)
			{
				response.IsValid = false;
				response.InvalidArgs = "no container on this game object";
				return response;
			}
			response.IsValid = true;
			return response;
		}
	}
}
