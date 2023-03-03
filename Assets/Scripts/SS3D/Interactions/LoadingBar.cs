using Coimbra;
using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using SS3D.Core.Behaviours;
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
        public float StartTime { get; set; }
        public float Duration { get; set; }
        public Slider TargetSlider;

        protected override void OnEnabled()
        {
            base.OnEnabled();

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
                GameObjectCache.Dispose(true);
                return;
            }

            TargetSlider.value = (Time.time - StartTime) / Duration;
        }
    }
}
