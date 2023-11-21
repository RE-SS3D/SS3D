using FishNet.Connection;
using SS3D.Permissions;
using UnityEngine;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    public class QuitCommand : Command
    {
        public override string LongDescription => "Close app";
        public override string ShortDescription => "Close app";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.User;
        public override CommandType Type => CommandType.Offline;
        
        private struct CalculatedValues : ICalculatedValues { }

        public override string Perform(string[] args, NetworkConnection conn = null)
        {
            if (!ReceiveCheckResponse(args, out CheckArgsResponse response, out CalculatedValues calculatedValues)) return response.InvalidArgs;

            #if UNITY_EDITOR
                        UnityEditor.EditorApplication.isPlaying = false;
            #else
                    Application.Quit();
            #endif
            return "Done";
        }
        protected override CheckArgsResponse CheckArgs(string[] args)
        {
            CheckArgsResponse response = new ();
            if (args.Length != 0) return response.MakeInvalid("Invalid number of arguments");

            return response.MakeValid(new CalculatedValues());
        }
    }
}