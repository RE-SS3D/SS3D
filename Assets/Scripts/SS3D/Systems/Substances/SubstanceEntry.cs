using System;

namespace SS3D.Substances
{
    [Serializable]
    public struct SubstanceEntry
    {
        public Substance Substance;
        public float Moles;

        public SubstanceEntry(Substance substance, float moles)
        {
            Substance = substance;
            Moles = moles;
        }
    }
}