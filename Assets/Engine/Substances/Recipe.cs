using System;
using UnityEngine;

namespace SS3D.Engine.Substances
{
    [Serializable]
    [CreateAssetMenu(menuName = "SS3D/Substances/Recipe")]
    public class Recipe : ScriptableObject
    {
        [Serializable]
        public struct RecipeComponent
        {
            public string Id;
            public float RelativeAmount;
        }

        public RecipeComponent[] Ingredients;
        public RecipeComponent[] Results;
        public float MinimalTemperature = float.MinValue;
        public float MaximalTemperature = float.MaxValue;
    }
}