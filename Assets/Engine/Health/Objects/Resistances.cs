using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SS3D.Engine.Health
{
    [Serializable]
    public class Resistances
    {
        // Immune to lava damage
        public bool LavaProof;

        // Immune to fire damage (but not necessarily lava or heat)
        public bool FireProof;

        // Instantly ignites from fire sources. Use this for objects like paper.
        public bool Flammable;

        // Acid can't even appear on it or melt it
        public bool UnAcidable;

        // Acid can get on it but not melt it
        public bool AcidProof;

        // Can't take damage
        public bool Indestructable;

        // Can't be frozen
        public bool FreezeProof;
    }
}