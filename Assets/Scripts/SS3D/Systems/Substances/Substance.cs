using System;
using UnityEngine;

namespace SS3D.Substances
{
    [Serializable]
    [CreateAssetMenu(menuName = "SS3D/Substances/Substance")]
    public class Substance : ScriptableObject
    {
        [field:SerializeField]
        public SubstanceType Type { get; set; }

        [field:SerializeField]
        public Color Color { get; set; }

        // Todo : Is this in "normal conditions" ? (1 atm, ambient room temperature around 27 celsius degree)
        [field:SerializeField]
        public float MillilitersPerMilliMoles { get; set; }
    }
}
