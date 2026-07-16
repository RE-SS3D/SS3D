using System.Collections.Generic;

namespace SS3D.Systems.Atmospherics.Pipes
{
    /// <summary>
    /// One connected gas pipe network with a well-mixed mole pool (design §6).
    /// </summary>
    public sealed class GasPipeNetworkRecord
    {
        public GasPipeNetworkId Id { get; }
        public HashSet<GasPipeSegmentKey> Segments { get; }
        public float[] Moles { get; }
        public float Temperature { get; set; }
        public float Volume { get; set; }

        public GasPipeNetworkRecord(GasPipeNetworkId id, int gasTypeCount)
        {
            Id = id;
            Segments = new HashSet<GasPipeSegmentKey>();
            Moles = new float[gasTypeCount];
            Temperature = AtmosConstants.StandardTemperature;
        }

        public float GetPressure(int gasTypeCount) =>
            AtmosPipeThermo.GetPressure(Moles, Temperature, Volume, gasTypeCount);

        public float GetTotalMoles(int gasTypeCount) =>
            AtmosPipeThermo.GetTotalMoles(Moles, gasTypeCount);

        public float GetHeatCapacity(float[] specificHeats, int gasTypeCount) =>
            AtmosPipeThermo.GetHeatCapacity(Moles, specificHeats, gasTypeCount);

        public void RecalculateVolume()
        {
            Volume = Segments.Count * AtmosPipeConstants.SegmentVolume;
        }
    }
}
