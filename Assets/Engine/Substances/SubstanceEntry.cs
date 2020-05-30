using System;

namespace SS3D.Engine.Substances
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