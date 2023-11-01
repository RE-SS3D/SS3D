using System;
using UnityEngine;

namespace SS3D.Substances
{
    [Serializable]
    [CreateAssetMenu(menuName = "SS3D/Substances/Substance")]
    public class Substance : ScriptableObject
    {
        public SubstanceType Type;
        public Color Color;

        // Todo : Is this in "normal conditions" ? (1 atm, ambient room temperature around 27 celsius degree)
        public float MillilitersPerMilliMoles;
    }
}
