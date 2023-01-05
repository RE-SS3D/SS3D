using SS3D.Attributes;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Core.Utils;
using SS3D.Systems.Entities;
using SS3D.Systems.PlayerControl;
using UnityEngine;
using UnityEngine.UI;

namespace SS3D.Systems.Health.UI
{
    [RequiredLayer("UI")]
    public class StaminaBarView : Actor
    {
        /// <summary>
        /// The actual UI bar
        /// </summary>
        [SerializeField] private Slider slider;

        /// <summary>
        /// Reference to the stamina controller that this view is supporting
        /// </summary>
        private StaminaController _controller;

        private void SetStamina(float stamina)
        {
            slider.value = stamina;
        }

        protected override void OnStart()
        {
            base.OnStart();
        }

        public void AssignViewToPlayer(StaminaController staminaController)
        {
            if (_controller != null) UnsubscribeFromEvents();
            _controller = staminaController;
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            _controller.OnStaminaChanged += SetStamina;
        }

        private void UnsubscribeFromEvents()
        {
            _controller.OnStaminaChanged -= SetStamina;
        }
    }
}