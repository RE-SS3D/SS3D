using Coimbra;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Screens
{
    [ProjectSettings("SS3D/UI", "Game Screen Settings")]
    public class GameScreenSettings : ScriptableSettings
    {
        [FormerlySerializedAs("ScaleInOutScale")]
        [SerializeField]
        private float _scaleInOutScale;

        [FormerlySerializedAs("FadeInOutDuration")]
        [SerializeField]
        private float _fadeInOutDuration;

        [FormerlySerializedAs("ScaleInOutDuration")]
        [SerializeField]
        private float _scaleInOutDuration;

        public float ScaleInOutScale => _scaleInOutScale;

        public float FadeInOutDuration => _fadeInOutDuration;

        public float ScaleInOutDuration => _scaleInOutDuration;
    }
}
