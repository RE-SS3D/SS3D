using System;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Substances
{
    /// <summary>
    /// Allocation-light mixture math for EditMode tests and container internals.
    /// Operates on a mutable <see cref="List{MixtureEntry}"/> of volumes in milliliters.
    /// </summary>
    public static class MixtureOperations
    {
        public static float TotalVolumeMl(IReadOnlyList<MixtureEntry> entries)
        {
            float total = 0f;
            for (int i = 0; i < entries.Count; i++)
            {
                total += entries[i].VolumeMl;
            }

            return total;
        }

        public static int IndexOf(IReadOnlyList<MixtureEntry> entries, string reagentId)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (string.Equals(entries[i].ReagentId, reagentId, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        public static float GetVolume(IReadOnlyList<MixtureEntry> entries, string reagentId)
        {
            int index = IndexOf(entries, reagentId);
            return index < 0 ? 0f : entries[index].VolumeMl;
        }

        public static float Add(List<MixtureEntry> entries, string reagentId, float volumeMl, float remainingCapacityMl)
        {
            if (string.IsNullOrEmpty(reagentId) || volumeMl <= 0f || remainingCapacityMl <= 0f)
            {
                return 0f;
            }

            float accepted = Mathf.Min(volumeMl, remainingCapacityMl);
            int index = IndexOf(entries, reagentId);
            if (index < 0)
            {
                entries.Add(new MixtureEntry(reagentId, accepted));
            }
            else
            {
                MixtureEntry entry = entries[index];
                entry.VolumeMl += accepted;
                entries[index] = entry;
            }

            return accepted;
        }

        public static float Remove(List<MixtureEntry> entries, string reagentId, float volumeMl)
        {
            int index = IndexOf(entries, reagentId);
            if (index < 0 || volumeMl <= 0f)
            {
                return 0f;
            }

            MixtureEntry entry = entries[index];
            float removed = Mathf.Min(entry.VolumeMl, volumeMl);
            entry.VolumeMl -= removed;
            if (entry.VolumeMl <= SubstanceConstants.VolumeEpsilonMl)
            {
                entries.RemoveAt(index);
            }
            else
            {
                entries[index] = entry;
            }

            return removed;
        }

        /// <summary>
        /// Removes a proportional slice of the whole mixture totaling <paramref name="volumeMl"/>.
        /// Returns entries that were removed (for transfer into another vessel).
        /// </summary>
        public static List<MixtureEntry> RemoveProportional(List<MixtureEntry> entries, float volumeMl)
        {
            var removed = new List<MixtureEntry>();
            float total = TotalVolumeMl(entries);
            if (total <= SubstanceConstants.VolumeEpsilonMl || volumeMl <= 0f)
            {
                return removed;
            }

            float fraction = Mathf.Clamp01(volumeMl / total);
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                MixtureEntry entry = entries[i];
                float take = entry.VolumeMl * fraction;
                if (take <= SubstanceConstants.VolumeEpsilonMl)
                {
                    continue;
                }

                removed.Add(new MixtureEntry(entry.ReagentId, take));
                entry.VolumeMl -= take;
                if (entry.VolumeMl <= SubstanceConstants.VolumeEpsilonMl)
                {
                    entries.RemoveAt(i);
                }
                else
                {
                    entries[i] = entry;
                }
            }

            return removed;
        }

        public static Color BlendColor(IReadOnlyList<MixtureEntry> entries, ReagentRegistry registry)
        {
            float total = TotalVolumeMl(entries);
            if (total <= SubstanceConstants.VolumeEpsilonMl || registry == null)
            {
                return new Color(1f, 1f, 1f, 0.2f);
            }

            Color blend = Color.clear;
            for (int i = 0; i < entries.Count; i++)
            {
                MixtureEntry entry = entries[i];
                if (!registry.TryGet(entry.ReagentId, out ReagentDefinition definition))
                {
                    continue;
                }

                float weight = entry.VolumeMl / total;
                blend += definition.Color * weight;
            }

            if (blend.a < 0.05f)
            {
                blend.a = 0.35f;
            }

            return blend;
        }

        public static float LowestFlashPointKelvin(IReadOnlyList<MixtureEntry> entries, ReagentRegistry registry)
        {
            float lowest = float.PositiveInfinity;
            if (registry == null)
            {
                return lowest;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                if (!registry.TryGet(entries[i].ReagentId, out ReagentDefinition definition))
                {
                    continue;
                }

                if (definition.FlashPointKelvin < lowest)
                {
                    lowest = definition.FlashPointKelvin;
                }
            }

            return lowest;
        }
    }
}
