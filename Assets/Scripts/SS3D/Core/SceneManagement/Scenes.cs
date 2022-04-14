using UnityEngine;

namespace SS3D.Core.SceneManagement
{
    [CreateAssetMenu(fileName = "Scenes", menuName = "Scenes", order = 0)]
    public sealed class Scenes : ScriptableObject
    {
        public SceneReference Startup;
    }
}