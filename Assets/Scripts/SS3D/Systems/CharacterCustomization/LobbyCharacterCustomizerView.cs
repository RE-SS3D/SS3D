using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Screens;
using SS3D.UI.Buttons;
using UnityEngine;
using UnityEngine.UI;

namespace SS3D.Systems.CharacterCustomization
{
    public class LobbyCharacterCustomizerView : NetworkedSpessBehaviour
    {
        [SerializeField] private Button _openCustomizerButton;
        [SerializeField] private LabelButton _closeCustomizerButton;

        protected override void OnStart()
        {
            base.OnStart();

            _openCustomizerButton.onClick.AddListener(HandleOpenCustomizerButton);
            _closeCustomizerButton.OnPressed += HandleCloseCustomizerButton;
        }

        private void OnDestroy()
        {
            _openCustomizerButton.onClick.RemoveListener(HandleOpenCustomizerButton);
            _closeCustomizerButton.OnPressed -= HandleCloseCustomizerButton;  
        }

        private void HandleOpenCustomizerButton()
        {
            ChangeGameScreenEvent changeGameScreenEvent = new(ScreenType.CharacterCustomizer);
            changeGameScreenEvent.Invoke(this);
        }

        private void HandleCloseCustomizerButton(bool state)
        {
            if (state)
            {
                return;
            }

            ChangeGameScreenEvent changeGameScreenEvent = new(ScreenType.Lobby);
            changeGameScreenEvent.Invoke(this);
        }
    }
}
