using UnityEngine;

namespace SS3D.Systems.Atmospherics
{
    /// <summary>
    /// SoA indexing helpers for per-cell gas mole buffers.
    /// </summary>
    public static class GasMixture
    {
        public static int GetMoleIndex(int cellIndex, GasId gasId)
        {
            return cellIndex * AtmosConstants.MaxGasTypes + gasId.Value;
        }

        public static int GetTotalMoleStride(int cellCount)
        {
            return cellCount * AtmosConstants.MaxGasTypes;
        }

        public static float GetMoles(float[] moles, int cellIndex, GasId gasId)
        {
            return moles[GetMoleIndex(cellIndex, gasId)];
        }

        public static void SetMoles(float[] moles, int cellIndex, GasId gasId, float value)
        {
            moles[GetMoleIndex(cellIndex, gasId)] = value;
        }
    }
}
