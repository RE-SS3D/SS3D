using System.Linq;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Gamemodes;
using SS3D.Systems.GameModes.Events;

namespace SS3D.Systems.Gamemodes.UI
{
    /// <summary>
    /// Client-side network bridge for per-player objective updates. Forwards
    /// objective changes to the objective list and shows the traitor indicator
    /// when the local player receives an antagonist objective. Target views are
    /// resolved through the ViewLocator instead of serialized references.
    /// </summary>
    public class GamemodeObjectivesNetworkView : NetworkView
    {
        private GamemodeObjectivePanelView _objectivePanelView;
        private TraitorIndicatorView _traitorIndicatorView;

        public override void OnStartClient()
        {
            base.OnStartClient();

            ClientManager.RegisterBroadcast<GamemodeObjectiveUpdatedMessage>(HandleGamemodeObjectiveUpdated);
        }

        private void HandleGamemodeObjectiveUpdated(GamemodeObjectiveUpdatedMessage m)
        {
            GamemodeObjective gamemodeObjective = m.Objective;

            _objectivePanelView = _objectivePanelView ? _objectivePanelView : ViewLocator.Get<GamemodeObjectivePanelView>()?.FirstOrDefault();
            _objectivePanelView?.ProcessObjectiveUpdated(gamemodeObjective);

            // Objective updates are only sent to the objective owner, so receiving
            // an antagonist objective means the local player is a traitor.
            if (gamemodeObjective.AlignmentRequirement != Alignment.Antagonists)
            {
                return;
            }

            _traitorIndicatorView = _traitorIndicatorView ? _traitorIndicatorView : ViewLocator.Get<TraitorIndicatorView>()?.FirstOrDefault();
            _traitorIndicatorView?.Show();
        }
    }
}