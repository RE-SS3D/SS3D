using System;
using UnityEngine;

namespace SS3D.Substances
{
    [Serializable]
    [CreateAssetMenu(menuName = "SS3D/Substances/Recipe")]
    public class Recipe : ScriptableObject
    {
        [Serializable]
        public struct RecipeComponent
        {
            public SubstanceType Type;
            public float RelativeAmount;
        }

        [field:SerializeField]
        public RecipeComponent[] Ingredients { get; private set; }

        [field:SerializeField]
        public RecipeComponent[] Results { get; private set; }

        [field:SerializeField]
        public float MinimalTemperature { get; private set; }

        [field:SerializeField]
        public float MaximalTemperature { get; private set; }
    }
}
