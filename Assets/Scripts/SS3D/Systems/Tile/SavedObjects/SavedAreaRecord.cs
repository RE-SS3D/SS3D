using System;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    [Serializable]
    public class SavedAreaRecord
    {
        // Legacy maps omit these flags and keep APC-generated defaults.
        public bool authored;
        public bool noApc;
        public ushort id;
        public string displayName;
        public string parentTag;
        public string ambienceProfileId;
        public Vector3 apcWorldPosition;
        public bool hasDepartmentalLightTint;
        public Color departmentalLightTint;
        public ulong defaultRequiredAccessBits;
        public bool lightingSwitchOn = true;
    }
}
