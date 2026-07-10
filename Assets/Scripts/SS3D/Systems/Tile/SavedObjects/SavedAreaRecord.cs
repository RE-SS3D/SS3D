using System;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    [Serializable]
    public class SavedAreaRecord
    {
        public ushort id;
        public string displayName;
        public string parentTag;
        public Vector3 apcWorldPosition;
    }
}
