using SS3D.Systems.Tile;
using System;

namespace SS3D.Systems.Persistence
{
    [Serializable]
    public sealed class SavedAreaChunkPayload
    {
        public SavedAreaRecord[] records;
    }
}
