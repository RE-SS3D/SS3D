using System.Linq;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.GameModes.Events;

namespace SS3D.Systems.Gamemodes.UI
{
    /// <summary>
    /// Client-side network bridge for round-wide gamemode announcements. These
    /// are broadcast to every client (unlike objective updates) and are shown
    /// through the round result banner, resolved via the ViewLocator.
    /// </summary>
    public class GamemodeAnnouncementNetworkView : NetworkView
    {
        private RoundResultView _roundResultView;

        public override void OnStartClient()
        {
            base.OnStartClient();

            ClientManager.RegisterBroadcast<GamemodeAnnouncementMessage>(HandleGamemodeAnnouncement);
        }

        private void HandleGamemodeAnnouncement(GamemodeAnnouncementMessage m)
        {
            _roundResultView = _roundResultView ? _roundResultView : ViewLocator.Get<RoundResultView>()?.FirstOrDefault();
            _roundResultView?.Show(m.Message);
        }
    }
}