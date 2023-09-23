using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using SS3D.Attributes;
using SS3D.Core.Behaviours;
using UnityEngine;
using UnityEngine.UI;

namespace SS3D.Systems.Health
{
    [RequiredLayer("UI")]
    public class StaminaBarView : View
    {
        /// <summary>
        /// The actual UI bar
        /// </summary>
        [SerializeField]
        private Slider _slider;

        /// <summary>
        /// Reference to the stamina controller that this view is supporting
        /// </summary>
        private StaminaController _controller;

        public void AssignViewToPlayer(StaminaController staminaController)
        {
            // Set the internal variable
            _controller = staminaController;
        }

        /// <summary>
        /// Unassigns the selected controller from the UI.
        /// </summary>
        /// <param name="staminaController">The StaminaController that will be unassigned</param>
        public void UnassignViewFromPlayer(StaminaController staminaController)
        {
            // All StaminaControllers in the scene can call this method, and the order is not guaranteed.
            // Therefore, we need to make sure the controller is actually assigned before clearing the field.
            if (_controller == staminaController)
            {
                _controller = null;
            }
        }

        protected override void OnStart()
        {
            base.OnStart();

            AddHandle(UpdateEvent.AddListener(HandleUpdate));
        }

        /// <summary>
        /// Set the slider to the correct value
        /// </summary>
        /// <param name="stamina">Proportion of stamina available (in range of 0f to 1f)</param>
        private void SetStamina(float stamina)
        {
            _slider.value = stamina;
        }

        private void HandleUpdate(ref EventContext context, in UpdateEvent updateEvent)
        {
            SetStamina(_controller ? _controller.CurrentStamina : 0f);
        }
    }
}