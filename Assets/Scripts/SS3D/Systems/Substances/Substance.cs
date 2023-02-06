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
        public float MillilitersPerMole;
    }
}
