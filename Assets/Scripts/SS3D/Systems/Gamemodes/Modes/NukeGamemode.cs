using System.Collections.Generic;
using SS3D.Logging;
using SS3D.Systems.Gamemodes;
using SS3D.Systems.Rounds;
using UnityEngine;

namespace SS3D.Systems.GameModes.Modes
{
    /// <summary>
    /// Nuclear Emergency (<c>antagonist-content.md</c> §6, effort <c>2026-11_round-end-loop</c> Decision 13):
    /// the squad shares one team objective, detonating the station's device. Crew hold no objective.
    /// The objective is met only when the round ends with the station destroyed.
    /// </summary>
    [CreateAssetMenu(menuName = "Gamemode/Modes/NukeGamemode", fileName = "NukeGamemode")]
    public class NukeGamemode : Gamemode
    {
        /// <summary>The squad's team objective: the first entry of <see cref="Gamemode.PossibleObjectives"/>.</summary>
        protected override void CreateObjectives(List<string> squadCkeys)
        {
            if (squadCkeys.Count == 0)
            {
                return;
            }

            if (PossibleObjectives == null || PossibleObjectives.Count == 0
                || !PossibleObjectives.TryGetAt(0, out GamemodeObjective teamObjective) || teamObjective == null)
            {
                Log.Error(this, "Nuke gamemode has no team objective configured", Logs.ServerOnly);
                return;
            }

            AssignTeamObjective(teamObjective, squadCkeys);
        }

        public override void ResolveObjectives(RoundEndRequest request)
        {
            if (request.StationDestroyed && RoundObjectives != null)
            {
                foreach (GamemodeObjective objective in RoundObjectives)
                {
                    objective.Succeed();
                }
            }

            base.ResolveObjectives(request);
        }
    }
}
