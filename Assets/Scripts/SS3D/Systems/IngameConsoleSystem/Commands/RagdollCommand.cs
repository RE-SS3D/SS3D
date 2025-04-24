using FishNet.Connection;
using FishNet.Object;
using SS3D.Core;
using SS3D.Permissions;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.PlayerControl;
using System.Globalization;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
	public class RagdollCommand : Command
	{
		public override string ShortDescription => "Toggle player's ragdoll";
        public override string Usage => "(ckey) [time]. Time is a float type and should be written as 0.5";
		public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
		public override CommandType Type => CommandType.Server;

        private record CalculatedValues(Entity Entity, float Time) : ICalculatedValues;

        [Server]
		public override string Perform(string[] args, NetworkConnection conn = null)
		{
            if (!ReceiveCheckResponse(args, out CheckArgsResponse response, out CalculatedValues values)) return response.InvalidArgs;
			Ragdoll ragdoll = values.Entity.GetComponent<Ragdoll>();

			if (args.Length > 1)
			{
				ragdoll.Knockdown(values.Time);
			}
			else
			{
				if (ragdoll.IsKnockedDown)
				{
					ragdoll.Recover();
				}
				else
				{
					ragdoll.KnockdownTimeless();
				}
			}
			return "Player ragdolled";
		}
        
		[Server]
		protected override CheckArgsResponse CheckArgs(string[] args)
		{
			CheckArgsResponse response = new();
			if (args.Length < 1 || args.Length > 2) return response.MakeInvalid("Invalid number of arguments");
			
			string ckey = args[0];
			Player player = SubSystems.Get<PlayerSubSystem>().GetPlayer(ckey);
			if (player == null) return response.MakeInvalid("This player doesn't exist");
			
			Entity entity = SubSystems.Get<EntitySubSystem>().GetSpawnedEntity(player);
			if (entity == null) return response.MakeInvalid("This entity doesn't exist");

            float time = 0;
			if (args.Length > 1)
			{
                // Use dot as separator
				NumberFormatInfo nfi = new();
				nfi.NumberDecimalSeparator = ".";
                if (float.TryParse(args[1], NumberStyles.Any, nfi, out time))
				{
					if (time <= 0) return response.MakeInvalid("Invalid time");
                }
				else return response.MakeInvalid("Invalid time");
            }
			return response.MakeValid(new CalculatedValues(entity, time));
		}
	}
}