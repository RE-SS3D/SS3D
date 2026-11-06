namespace SS3D.Systems.Atmospherics.Pipes
{
    /// <summary>
    /// A gas mix as relative amounts indexed by gas id, as gas markers store it.
    /// </summary>
    public static class AtmosGasMix
    {
        private const float MatchTolerance = 0.001f;

        public static float[] Vacuum() => new float[AtmosConstants.DefaultGasCount];

        public static float[] Air()
        {
            float[] fractions = Vacuum();
            fractions[AtmosConstants.Oxygen.Value] = AtmosConstants.StationOxygenMoles;
            fractions[AtmosConstants.Nitrogen.Value] = AtmosConstants.StationNitrogenMoles;
            return fractions;
        }

        public static float[] Pure(GasId gas)
        {
            float[] fractions = Vacuum();
            fractions[gas.Value] = 1f;
            return fractions;
        }

        /// <summary>True when the mix holds nothing, however long its array is.</summary>
        public static bool IsVacuum(float[] fractions) => Total(fractions) <= 0f;

        /// <summary>True when two mixes hold the same gases in the same proportions.</summary>
        public static bool Matches(float[] a, float[] b)
        {
            float totalA = Total(a);
            float totalB = Total(b);
            if (totalA <= 0f || totalB <= 0f)
                return totalA <= 0f && totalB <= 0f;

            for (int id = 0; id < System.Math.Max(a.Length, b.Length); id++)
            {
                float shareA = id < a.Length ? a[id] / totalA : 0f;
                float shareB = id < b.Length ? b[id] / totalB : 0f;
                if (System.Math.Abs(shareA - shareB) > MatchTolerance)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Writes <paramref name="totalMoles"/> of the mix into a buffer indexed by gas id, clearing the other gases.
        /// Gases past the end of the buffer are left out of the total too, so the pressure still comes out right.
        /// </summary>
        public static void FillMoles(float[] fractions, float totalMoles, float[] moles)
        {
            System.Array.Clear(moles, 0, moles.Length);
            int count = System.Math.Min(fractions?.Length ?? 0, moles.Length);
            float total = 0f;
            for (int id = 0; id < count; id++)
                total += fractions[id];

            if (total <= 0f)
                return;

            for (int id = 0; id < count; id++)
                moles[id] = totalMoles * fractions[id] / total;
        }

        private static float Total(float[] fractions)
        {
            float total = 0f;
            if (fractions != null)
            {
                foreach (float fraction in fractions)
                    total += fraction;
            }

            return total;
        }
    }
}
