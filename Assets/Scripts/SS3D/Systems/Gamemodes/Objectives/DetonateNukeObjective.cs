using UnityEngine;

namespace SS3D.Systems.Gamemodes.Objectives
{
    /// <summary>
    /// The Nuke Ops team objective: detonate the station's device (<c>antagonist-content.md</c> §6). It is
    /// resolved when the round ends, by <see cref="SS3D.Systems.GameModes.Modes.NukeGamemode.ResolveObjectives"/>:
    /// met only if the round ended with the station destroyed.
    /// </summary>
    [CreateAssetMenu(menuName = "Gamemode/Objectives/DetonateNuke", fileName = "DetonateNuke")]
    public class DetonateNukeObjective : GamemodeObjective { }
}
