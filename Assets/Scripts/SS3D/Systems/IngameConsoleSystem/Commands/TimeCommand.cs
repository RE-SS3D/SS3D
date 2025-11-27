using FishNet.Connection;
using SS3D.Permissions;
using System.Diagnostics;
using System.Globalization;
using UnityEngine;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    public class TimeCommand : Command
    {
        private record CalculatedValues(float Time) : ICalculatedValues;

        public override string ShortDescription => "Restart app";

        public override ServerRoleTypes AccessLevel => ServerRoleTypes.User;

        public override CommandType Type => CommandType.Offline;

        public override string Perform(string[] args, NetworkConnection conn = null)
        {
            if (!ReceiveCheckResponse(args, out CheckArgsResponse response, out CalculatedValues values))
            {
                return response.InvalidArgs;
            }

            Time.timeScale = values.Time;

            return $"timescale set to {Time.timeScale}";
        }

        protected override CheckArgsResponse CheckArgs(string[] args)
        {
            CheckArgsResponse response = default;
            if (args.Length != 1)
            {
                return response.MakeInvalid("Invalid number of arguments");
            }

            // Use dot as separator
            NumberFormatInfo nfi = new();
            nfi.NumberDecimalSeparator = ".";
            if (float.TryParse(args[0], NumberStyles.Any, nfi, out float time))
            {
                if (time <= 0)
                {
                    return response.MakeInvalid("Invalid time");
                }
            }
            else
            {
                return response.MakeInvalid("Invalid time");
            }

            return response.MakeValid(new CalculatedValues(time));
        }
    }
}
