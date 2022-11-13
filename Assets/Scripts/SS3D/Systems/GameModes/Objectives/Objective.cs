using UnityEngine;

namespace SS3D.Systems.GameModes.Objectives
{
    [CreateAssetMenu(fileName = "Objective", menuName = "GameModes/Objective", order = 1)]
    public class Objective : ScriptableObject
    {
        public string Title;
        private bool Done;

        bool IsDone()
        {
            return Done;
        }
    }
}
