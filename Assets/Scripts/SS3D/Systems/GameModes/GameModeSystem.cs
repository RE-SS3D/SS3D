using SS3D.Core.Behaviours;
using UnityEngine;
using System.Collections.Generic;
using SS3D.Systems.GameModes.Objectives;

namespace SS3D.Systems.GameModes
{
    public sealed class GameModeSystem : NetworkedSystem
    {
        public GetNukeAuthCard GetNukeAuthCard = new GetNukeAuthCard();

        private void Awake()
        {
            GetNukeAuthCard.InitializeObjective();
        }
    }
}
