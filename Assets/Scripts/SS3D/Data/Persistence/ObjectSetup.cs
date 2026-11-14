using SS3D.Logging;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Data.Persistence
{
    /// <summary>
    /// Captures and restores the authored setup of one placed object: every <see cref="IPersistentSetup"/> on it, keyed by
    /// <see cref="IPersistentSetup.SetupKey"/> (Documents/architecture/2026-11_object-setup-persistence.md Q10–Q13).
    /// Items stored in the object's containers are not part of its components (see <see cref="ISetupRecordRoot"/>); a
    /// container captures them as its own contents.
    /// </summary>
    public static class ObjectSetup
    {
        private static readonly List<IPersistentSetup> Buffer = new();

        /// <summary>Where nested restores report what they couldn't put back, for the map's load report.</summary>
        [ThreadStatic]
        private static Action<string> _currentReport;

        /// <summary>The setup that differs from the prefab default; empty when the object is at its defaults.</summary>
        public static SavedSetupEntry[] Capture(GameObject root)
        {
            if (root == null)
                return Array.Empty<SavedSetupEntry>();

            List<SavedSetupEntry> entries = null;
            HashSet<string> keys = null;
            foreach (IPersistentSetup component in CollectComponents(root))
            {
                string key = component.SetupKey;
                if (string.IsNullOrEmpty(key))
                    continue;

                keys ??= new HashSet<string>();
                if (!keys.Add(key))
                {
                    Log.Warning(typeof(ObjectSetup), "{object} has two setup components keyed {key}; only the first is saved",
                        Logs.ServerOnly, root.name, key);
                    continue;
                }

                string json = component.CaptureSetup();
                if (string.IsNullOrEmpty(json))
                    continue;

                (entries ??= new List<SavedSetupEntry>()).Add(new SavedSetupEntry(key, json));
            }

            return entries?.ToArray() ?? Array.Empty<SavedSetupEntry>();
        }

        /// <summary>
        /// Applies <paramref name="entries"/> to the object's setup components. An entry with no matching component, or
        /// with JSON its component can't read, is skipped and reported once per object as "<paramref name="assetName"/>
        /// settings"; the object keeps its defaults for it.
        /// </summary>
        /// <param name="reportSkipped">Receives what couldn't be restored, nested container contents included.</param>
        /// <param name="resetUnlisted">Return components without an entry to their default (undo and redo).</param>
        /// <returns>False when some entry could not be restored.</returns>
        public static bool Restore(GameObject root, IReadOnlyList<SavedSetupEntry> entries, string assetName = null,
            Action<string> reportSkipped = null, bool resetUnlisted = false)
        {
            bool hasEntries = entries != null && entries.Count > 0;
            if (root == null || (!hasEntries && !resetUnlisted))
                return true;

            Action<string> previous = _currentReport;
            if (reportSkipped != null)
                _currentReport = reportSkipped;

            try
            {
                Dictionary<string, IPersistentSetup> byKey = new();
                foreach (IPersistentSetup component in CollectComponents(root))
                {
                    if (!string.IsNullOrEmpty(component.SetupKey))
                        byKey.TryAdd(component.SetupKey, component);
                }

                bool failed = false;
                HashSet<string> restored = new();
                if (hasEntries)
                {
                    foreach (SavedSetupEntry entry in entries)
                    {
                        if (string.IsNullOrEmpty(entry.key) || !byKey.TryGetValue(entry.key, out IPersistentSetup component))
                        {
                            Log.Warning(typeof(ObjectSetup), "{object} has no setup component keyed {key}; that setting is skipped",
                                Logs.ServerOnly, root.name, entry.key);
                            failed = true;
                            continue;
                        }

                        if (!restored.Add(entry.key))
                            continue;

                        if (!TryRestore(root, component, entry.json))
                            failed = true;
                    }
                }

                if (resetUnlisted)
                {
                    foreach (KeyValuePair<string, IPersistentSetup> pair in byKey)
                    {
                        if (!restored.Contains(pair.Key))
                            TryRestore(root, pair.Value, null);
                    }
                }

                if (failed)
                    ReportSkipped($"{(string.IsNullOrEmpty(assetName) ? root.name : assetName)} settings");

                return !failed;
            }
            finally
            {
                _currentReport = previous;
            }
        }

        /// <summary>Counts something a restore couldn't put back (a stored item of a removed type) in the active load report.</summary>
        public static void ReportSkipped(string what) => _currentReport?.Invoke(what);

        /// <summary>Same entries, in any order.</summary>
        public static bool SameSetup(IReadOnlyList<SavedSetupEntry> a, IReadOnlyList<SavedSetupEntry> b)
        {
            int countA = a?.Count ?? 0;
            int countB = b?.Count ?? 0;
            if (countA != countB)
                return false;

            if (countA == 0)
                return true;

            var remaining = new List<SavedSetupEntry>(b);
            foreach (SavedSetupEntry entry in a)
            {
                if (!remaining.Remove(entry))
                    return false;
            }

            return true;
        }

        private static bool TryRestore(GameObject root, IPersistentSetup component, string json)
        {
            try
            {
                component.RestoreSetup(json);
                return true;
            }
            catch (Exception exception)
            {
                Log.Warning(typeof(ObjectSetup), "Could not restore {key} on {object}: {error}",
                    Logs.ServerOnly, component.SetupKey, root.name, exception.Message);
                return false;
            }
        }

        /// <summary>
        /// The object's own setup components. A stored item is parented under its container, so components whose nearest
        /// <see cref="ISetupRecordRoot"/> is not the root's own are left out: they belong to the stored item's record.
        /// </summary>
        private static List<IPersistentSetup> CollectComponents(GameObject root)
        {
            var result = new List<IPersistentSetup>();
            Buffer.Clear();
            root.GetComponentsInChildren(true, Buffer);
            if (Buffer.Count == 0)
                return result;

            ISetupRecordRoot ownRecord = root.GetComponent<ISetupRecordRoot>();
            foreach (IPersistentSetup setup in Buffer)
            {
                if (setup is Component component && !ReferenceEquals(component.GetComponentInParent<ISetupRecordRoot>(true), ownRecord))
                    continue;

                result.Add(setup);
            }

            Buffer.Clear();
            return result;
        }
    }
}
