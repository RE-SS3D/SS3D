using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SS3D.Engine.Health
{
    [Serializable]
    public class Resistances
    {
        
        public bool LavaProof;      // Immune to lava damage
        public bool FireProof;      // Immune to fire damage (but not necessarily lava or heat)
        public bool Flammable;      // Instantly ignites from fire sources. Use this for objects like paper.
        public bool UnAcidable;     // Acid can't even appear on it or melt it
        public bool AcidProof;      // Acid can get on it but not melt it
        public bool Indestructable; // Can't take damage
        public bool FreezeProof;    // Can't be frozen
    }
}