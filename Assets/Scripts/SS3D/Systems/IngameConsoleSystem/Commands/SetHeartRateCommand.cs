using FishNet.Connection;
using FishNet.Object;
using SS3D.Permissions;
using System.Globalization;
using UnityEngine;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    /// <summary>
    /// Set a heart's rate, which is the only way to exercise the supply side of the circulatory model at runtime.
    /// Cardiac output scales with rate - CardiacOutputFactor is BPM/60 - so 0 stops delivery entirely and the body
    /// starves on whatever its tissues hold in reserve, while 120 doubles the flow cap.
    /// Added for #1362's negative controls: until this existed Heart.SetBeatFrequency had no callers at all, so heart
    /// rate could not be changed in game and none of "stopped heart suffocates the body", "oxygen starvation causes
    /// bleeding", or "supply tracks heart rate" could be observed rather than reasoned about.
    /// </summary>
    public class SetHeartRateCommand : Command
    {
        public override string LongDescription => "Set a heart's rate in beats per minute. 0 stops it: no oxygen is delivered and the body starves on its reserves.";
        public override string ShortDescription => "Set a heart's rate";
        public override string Usage => "(game object name) (beats per minute)";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
        public override CommandType Type => CommandType.Server;

        private record CalculatedValues(Health.Heart Heart, float BeatsPerMinute) : ICalculatedValues;

        [Server]
        public override string Perform(string[] args, NetworkConnection conn = null)
        {
            if (!ReceiveCheckResponse(args, out CheckArgsResponse response, out CalculatedValues values)) return response.InvalidArgs;

            values.Heart.SetBeatFrequency(values.BeatsPerMinute);

            return $"{values.Heart.Name} set to {values.BeatsPerMinute} BPM";
        }

        [Server]
        protected override CheckArgsResponse CheckArgs(string[] args)
        {
            CheckArgsResponse response = new();

            if (args.Length != 2) return response.MakeInvalid("Invalid number of arguments");

            GameObject go = GameObject.Find(args[0]);
            if (go == null) return response.MakeInvalid("No object with this name");

            Health.Heart heart = go.GetComponent<Health.Heart>();
            if (heart == null) return response.MakeInvalid("That object is not a heart");

            if (!float.TryParse(args[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float beatsPerMinute) || beatsPerMinute < 0f)
            {
                return response.MakeInvalid("Beats per minute must be a number of 0 or more");
            }

            return response.MakeValid(new CalculatedValues(heart, beatsPerMinute));
        }
    }
}