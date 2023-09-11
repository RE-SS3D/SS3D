using Coimbra;
using UnityEngine.Serialization;

namespace SS3D.Systems.Screens
{
    [ProjectSettings("SS3D/UI", "Game Screen Settings")]
    public class GameScreenSettings : ScriptableSettings
    {
        public float ScaleInOutScale;

        public float FadeInOutDuration;
        public float ScaleInOutDuration;
    }
}