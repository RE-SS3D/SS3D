using JetBrains.Annotations;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Gamemodes
{
    /// <summary>
    /// Used to store a list of GamemodeObjectiveCollectionEntry, in order to quick change them.
    /// </summary>
    [CreateAssetMenu(menuName = "Gamemode/GamemodeObjectiveCollection", fileName = "Gamemode/GamemodeObjectiveCollection", order = 0)]
    public class GamemodeObjectiveCollection : ScriptableObject
    {
        /// <summary>
        /// All the objectives in this collection.
        /// </summary>
        public List<GamemodeObjectiveCollectionEntry> Entries { get; private set; }

        /// <summary>
        /// The amount of objectives in this collection.
        /// </summary>
        public int Count => Entries.Count;

        [NotNull]
        public static GamemodeObjectiveCollection CreateInstance(List<GamemodeObjectiveCollectionEntry> entries)
        {
            GamemodeObjectiveCollection data = ScriptableObject.CreateInstance<GamemodeObjectiveCollection>();
            data.Init(entries);
            return data;
        }

        /// <summary>
        /// Clones the collection entry so you don't modify the SO file.
        /// </summary>
        /// <returns>The cloned collection.</returns>
        public GamemodeObjectiveCollection Clone()
        {
            GamemodeObjectiveCollection clone = Instantiate(this);

            List<GamemodeObjectiveCollectionEntry> entriesClone = new(Entries);
            clone.Entries = entriesClone;

            return clone;
        }

        /// <summary>
        /// Gets an objective at an index
        /// </summary>
        /// <returns></returns>
        public bool TryGetAt(int index, out GamemodeObjective objective, bool useAssignmentRestrictions = false)
        {
            bool hasObjective = Entries[index].TryGetObjective(out objective, useAssignmentRestrictions);

            return hasObjective;
        }

        private void Init(List<GamemodeObjectiveCollectionEntry> entries)
        {
            Entries = entries;
        }
    }
}
