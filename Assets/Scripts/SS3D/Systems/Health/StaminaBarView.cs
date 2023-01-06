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

        /// <summary>
        /// Set the slider to the correct value
        /// </summary>
        /// <param name="stamina">Proportion of stamina available (in range of 0f to 1f)</param>
        private void SetStamina(float stamina)
        {
            slider.value = stamina;
        }

        protected override void OnStart()
        {
            base.OnStart();
        }

        protected override void HandleUpdate(in float deltaTime)
        {
            SetStamina(_controller ? _controller.Current : 0f);
        }


        public void AssignViewToPlayer(StaminaController staminaController)
        {
            // Set the internal variable
            _controller = staminaController;
        }
    }
}