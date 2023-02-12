using Coimbra.Services.Events;
using FishNet.Object;
using SS3D.Logging;
using SS3D.Systems.Rounds.Events;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.GameModes.Modes
{
    /// <summary>
    /// This gamemode is a basic test of the gamemode system, it just distributes the nuke objective to every player.
    /// </summary>
    [CreateAssetMenu(menuName = "Gamemode/Modes/NukeGamemode", fileName = "NukeGamemode")]
    public class NukeGamemode : Gamemode { }
}
