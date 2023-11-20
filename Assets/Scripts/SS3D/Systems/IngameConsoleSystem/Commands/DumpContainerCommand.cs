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
        private struct CalculatedValues : ICalculatedValues
        {
            public readonly AttachedContainer Container;
            public CalculatedValues(AttachedContainer container)
            {
                Container = container;
            }
        }

		public override string Perform(string[] args, NetworkConnection conn)
		{
            if (!ReceiveCheckResponse(args, out CheckArgsResponse response, out CalculatedValues calculatedValues)) return response.InvalidArgs;
            
			calculatedValues.Container.Dump();
			return "Container content dumped";
		}
		protected override CheckArgsResponse CheckArgs(string[] args)
		{
			CheckArgsResponse response = new();
			if (args.Length != 1) return response.MakeInvalid("Invalid number of arguments");
			
			GameObject containerGo = GameObject.Find(args[0]);
			if (containerGo == null) return response.MakeInvalid("This container doesn't exist");
            
            AttachedContainer container =  containerGo.GetComponentInChildren<AttachedContainer>();
			if (container == null) return response.MakeInvalid("No container on this game object");
			
			response.IsValid = true;
			return response.MakeValid(new CalculatedValues(container));
		}
	}
}
