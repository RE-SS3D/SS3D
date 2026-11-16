using SS3D.Data.Management;
using SS3D.Data.Persistence;
using UnityEngine;

namespace SS3D.Systems.Persistence
{
    public static class RoundHistoryStore
    {
        public static bool Append(RoundHistoryEntry entry)
        {
            if (entry == null)
            {
                return false;
            }

            return LocalStorage.AppendJsonlLine(PersistencePaths.RoundHistory, JsonUtility.ToJson(entry));
        }
    }
}
