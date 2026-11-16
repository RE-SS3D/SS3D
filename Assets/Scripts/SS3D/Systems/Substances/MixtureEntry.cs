using System;

namespace SS3D.Systems.Substances
{
    [Serializable]
    public struct MixtureEntry
    {
        public string ReagentId;
        public float VolumeMl;

        public MixtureEntry(string reagentId, float volumeMl)
        {
            ReagentId = reagentId;
            VolumeMl = volumeMl;
        }
    }
}
