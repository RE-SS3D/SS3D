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
		public override string LongDescription => "ragdoll (user ckey) [time]";
		public override string ShortDescription => "Toggle player's ragdoll. Time is a float type and should be written as 0.5";
		public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
		public override CommandType Type => CommandType.Server;
		
		[Server]
		public override string Perform(string[] args, NetworkConnection conn = null)
		{
			CheckArgsResponse checkArgsResponse = CheckArgs(args);
			if (checkArgsResponse.IsValid == false)
				return checkArgsResponse.InvalidArgs;
			string ckey = args[0];
			Player player = Subsystems.Get<PlayerSystem>().GetPlayer(ckey);
			Entity entity = Subsystems.Get<EntitySystem>().GetSpawnedEntity(player);
			Ragdoll ragdoll = entity.GetComponent<Ragdoll>();

			if (args.Length > 1)
			{
				// Force c# into using dot as separator
				float time = float.Parse(args[1], CultureInfo.InvariantCulture);
				ragdoll.Knockdown(time);
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
			if (args.Length < 1 || args.Length > 2)
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
			Entity entity = Subsystems.Get<EntitySystem>().GetSpawnedEntity(player);
			if (entity == null)
			{
				response.IsValid = false;
				response.InvalidArgs = "This entity doesn't exist";
				return response;
			}
			response.IsValid = true;
			// Use dot as separator
			if (args.Length > 1)
			{
				NumberFormatInfo nfi = new();
				nfi.NumberDecimalSeparator = ".";

				if (float.TryParse(args[1], NumberStyles.Any, nfi, out float time))
				{
					if (time <= 0)
					{
						response.IsValid = false;
						response.InvalidArgs = "Invalid time";

						return response;
					}
				}
				else
				{
					response.IsValid = false;
					response.InvalidArgs = "Invalid time";

					return response;
				}
			}

			return response;
		}
	}
}