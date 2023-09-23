using Coimbra;
using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using UnityEngine;
using UnityEngine.UI;
using Actor = SS3D.Core.Behaviours.Actor;

namespace SS3D.Interactions
{
    /// <summary>
    /// Manages loading bar progress, should be attached to loading bar prefab
    /// </summary>
    public class LoadingBar : Actor
    {
        public Slider TargetSlider;

        public float StartTime { get; set; }

        public float Duration { get; set; }

        protected override void OnStart()
        {
            base.OnStart();

            AddHandle(UpdateEvent.AddListener(HandleUpdate));
            Setup();
        }

        private void HandleUpdate(ref EventContext context, in UpdateEvent updateEvent)
        {
            UpdateLoadingProgressTask();
        }

        private void Setup()
        {
            if (StartTime == 0)
            {
                StartTime = Time.time;
            }
        }

        /// <summary>
        /// Updates the loading progress
        /// </summary>
        private void UpdateLoadingProgressTask()
        {
            if (StartTime + Duration < Time.time)
            {
                GameObject.Dispose(true);
                return;
            }

            TargetSlider.value = (Time.time - StartTime) / Duration;
        }
    }
}
