using System;

namespace SS3D.Data.Persistence
{
    /// <summary>
    /// One component's authored setup on a saved object: the <see cref="IPersistentSetup.SetupKey"/> it came from and
    /// the JSON that component wrote. JSON in a string, like <see cref="PersistenceChunk.payloadJson"/>, so each level of
    /// nested container contents is its own string and <c>JsonUtility</c>'s depth limit never applies.
    /// </summary>
    [Serializable]
    public struct SavedSetupEntry : IEquatable<SavedSetupEntry>
    {
        public string key;

        public string json;

        public SavedSetupEntry(string key, string json)
        {
            this.key = key;
            this.json = json;
        }

        public bool Equals(SavedSetupEntry other) => key == other.key && json == other.json;

        public override bool Equals(object obj) => obj is SavedSetupEntry other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(key, json);
    }
}
