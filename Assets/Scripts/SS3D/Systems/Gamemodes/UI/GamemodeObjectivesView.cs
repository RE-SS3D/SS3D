using System.Collections.Generic;
using SS3D.Core.Behaviours;
using SS3D.Systems.Gamemodes;
using SS3D.Systems.GameModes.Events;
using TMPro;
using UnityEngine;

namespace SS3D.Systems.GameModes.UI
{
    public class GamemodeObjectivesView : NetworkedSpessBehaviour
    {
        private GamemodeObjectivePanelView _objectivePanelView;

        protected override void OnStart()
        {
            base.OnStart();

            ClientManager.RegisterBroadcast<GamemodeObjectiveUpdatedMessage>(HandleGamemodeObjectiveUpdated);
        }

        private void HandleGamemodeObjectiveUpdated(GamemodeObjectiveUpdatedMessage m)
        {
            GamemodeObjective gamemodeObjective = m.Objective;

            _objectivePanelView.ProcessObjectiveUpdated(gamemodeObjective);
        }
    }
}