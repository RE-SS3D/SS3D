using UnityEngine;
using UnityEngine.UI;

namespace SS3D.Engine.Interactions
{
    /// <summary>
    /// Manages loading bar progress, should be attached to loading bar prefab
    /// </summary>
    public class LoadingBar : MonoBehaviour
    {
        public float StartTime { get; set; }
        public float Duration { get; set; }
        public Slider TargetSlider;
        
        void Start()
        {
            // Set start time to now if not specified
            if (StartTime == 0)
            {
                StartTime = Time.time;
            }
        }
        
        void Update()
        {
            // Check if loading completed
            if (StartTime + Duration < Time.time)
            {
                Destroy(gameObject);
                return;
            }

            // Adjust slider
            TargetSlider.value = (Time.time - StartTime) / Duration;
        }
    }
}
