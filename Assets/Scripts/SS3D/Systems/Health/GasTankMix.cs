using System;

namespace SS3D.Systems.Health
{
    /// <summary>
    /// Moles per gas inside a <see cref="GasTankItemExtension"/>. There is no pressure model: a tank
    /// is a finite supply with a mix (armor.md §3).
    /// </summary>
    [Serializable]
    public struct GasTankMix
    {
        public float OxygenMoles;
        public float PlasmaMoles;

        public float TotalMoles => OxygenMoles + PlasmaMoles;

        public float OxygenFraction => TotalMoles > 0f ? OxygenMoles / TotalMoles : 0f;

        public float PlasmaFraction => TotalMoles > 0f ? PlasmaMoles / TotalMoles : 0f;

        /// <summary>The mix left after drawing <paramref name="moles"/> evenly across its gases.</summary>
        public GasTankMix Without(float moles)
        {
            float total = TotalMoles;
            if (total <= moles)
            {
                return default;
            }

            float kept = (total - moles) / total;
            return new GasTankMix { OxygenMoles = OxygenMoles * kept, PlasmaMoles = PlasmaMoles * kept };
        }
    }
}
