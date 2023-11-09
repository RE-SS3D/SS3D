using System;

namespace SS3D.Substances
{
    [Serializable]
    public struct SubstanceEntry
    {
        public Substance Substance;
        public float MilliMoles;

        public SubstanceEntry(Substance substance, float millimoles)
        {
            Substance = substance;
            MilliMoles = millimoles;
        }
    }
}