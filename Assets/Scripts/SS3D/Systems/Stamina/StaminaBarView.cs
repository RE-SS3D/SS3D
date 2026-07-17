using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using SS3D.Attributes;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Humanoid;
using SS3D.UI.Settings;
using System.Linq;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.UI;

namespace SS3D.Systems.Stamina
{
    [RequiredLayer("UI")]
    public class StaminaBarView : View
    {
        [SerializeField] private Slider _slider;
        private StaminaController _controller;

        protected override void OnStart()
        {
            base.OnStart();
            AddHandle(UpdateEvent.AddListener(HandleUpdate));
            var uiVisibilityView = ViewLocator.Get<GlobalUiVisibilityControllerView>().First();
            uiVisibilityView.RegisterToggle(GameObject);
        }

        private void SetStamina(float stamina)
        {
            _slider.value = stamina;
        }

        private void HandleUpdate(ref EventContext context, in UpdateEvent updateEvent)
        {
            SetStamina(_controller ? _controller.CurrentStamina : 0f);
        }

        public void AssignViewToPlayer(StaminaController staminaController)
        {
            _controller = staminaController;
        }

        public void UnassignViewFromPlayer(StaminaController staminaController)
        {
            if (_controller == staminaController)
            {
                _controller = null;
            }
        }
    }
}
