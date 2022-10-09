using FishNet;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.PlayerControl;
using SS3D.Systems.Rounds.Messages;
using SS3D.UI;
using SS3D.UI.Buttons;
using UnityEngine;

namespace SS3D.Systems.Lobby.UI
{
    public class LobbyReadyView : NetworkedSpessBehaviour
    {
        [SerializeField] private ToggleLabelButton _button;

        protected override void OnStart()
        {
            base.OnStart();

            _button.OnPressed += HandleButtonPressed;
        }

        private void HandleButtonPressed(bool pressed, MouseButtonType mouseButtonType)
        {
            if (mouseButtonType == MouseButtonType.MouseUp)
            {
                return;
            }

            string ckey = GameSystems.Get<PlayerControlSystem>().GetSoulCkeyByConn(LocalConnection);
                           
            ChangePlayerReadyMessage playerReadyMessage = new(ckey, pressed);

            InstanceFinder.ClientManager.Broadcast(playerReadyMessage);
        }
    }
}
