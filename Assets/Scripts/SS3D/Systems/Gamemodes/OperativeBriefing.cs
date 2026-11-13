using System.Collections.Generic;
using System.Linq;

namespace SS3D.Systems.Gamemodes
{
    /// <summary>
    /// The private spawn briefing for an antagonist (<c>objectives.md</c> §8): their role, their objectives
    /// in plain language and who else is on their team. Pure, so EditMode tests cover the wording.
    /// </summary>
    public static class OperativeBriefing
    {
        public const string Header = "BRIEFING";

        /// <summary>SS14/tgstation nukeops_start.ogg, played to each operative with their briefing.</summary>
        public const string SoundId = "ab3f35a2b6ac4936a6b951309a105e7d";

        public static string Text(string role, IEnumerable<string> objectives, IEnumerable<string> squadmates)
        {
            string[] goals = (objectives ?? Enumerable.Empty<string>()).Where(o => !string.IsNullOrWhiteSpace(o)).ToArray();
            string[] team = (squadmates ?? Enumerable.Empty<string>()).Where(n => !string.IsNullOrWhiteSpace(n)).ToArray();

            string objectiveText = goals.Length == 0 ? "No objective." : "Objective: " + string.Join("; ", goals) + ".";
            string teamText = team.Length == 0 ? "You are operating alone." : "Your squad: " + string.Join(", ", team) + ".";
            return $"You are a {role}. {objectiveText} {teamText}";
        }
    }
}
