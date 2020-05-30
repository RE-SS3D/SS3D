using System;
using UnityEngine;

namespace SS3D.Engine.Substances
{
    [Serializable]
    [CreateAssetMenu(menuName = "Substance")]
    public class Substance : ScriptableObject
    {
        public string Id;
        public Color Color;
        public float MillilitersPerMole;
    }
}
