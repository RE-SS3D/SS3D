using SS3D.Core.Behaviours;
using UnityEngine;
using System.Collections.Generic;
using SS3D.Systems.GameModes.Objectives;

namespace SS3D.Systems.GameModes
{
    public sealed class GameModeSystem : NetworkedSystem
    {
        public List<Objective> Objectives;

        private void Start()
        {
            Debug.Log("Initializing Game Mode System...");
        }
    }
}
