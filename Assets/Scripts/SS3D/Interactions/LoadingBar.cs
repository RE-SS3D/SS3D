using Coimbra;
using SS3D.Core.Behaviours;
using UnityEngine;
using UnityEngine.UI;

namespace SS3D.Interactions
{
    /// <summary>
    /// Manages loading bar progress, should be attached to loading bar prefab
    /// </summary>
    public class LoadingBar : SpessBehaviour
    {
        public float StartTime { get; set; }
        public float Duration { get; set; }
        public Slider TargetSlider;

        protected override void OnStart()
        {
            base.OnStart();

            Setup();
        }

        protected override void HandleUpdate(in float deltaTime)
        {
            base.HandleUpdate(in deltaTime);

            ProcessLoading();
        }

        private void Setup()
        {
            if (StartTime == 0)
            {
                StartTime = Time.time;
            }   
        }


        private void ProcessLoading()
        {
            if (StartTime + Duration < Time.time)
            {
                GameObjectCache.Destroy();
                return;
            }

            TargetSlider.value = (Time.time - StartTime) / Duration;
        }
    }
}
