using SS3D.Systems.Health;
using UnityEngine;

namespace SS3D.Systems.Inventory.Containers
{
    /// <summary>
    /// Shared rules for taking items from another character's worn/held gear
    /// (Documents/design/inventory-storage.md §9; examine hold-to-take). Anyone in reach can be taken
    /// from; an alert target takes longer, a blind reach longer still, and is warned. Restrained is
    /// deferred until restraints exist.
    /// </summary>
    public static class CharacterLootUtility
    {
        /// <summary>Take from a dead or unconscious character (inventory-storage.md §9: easy).</summary>
        public const float IncapacitatedTakeSeconds = 1.5f;

        /// <summary>Take a visible item from a conscious character, who sees it coming.</summary>
        public const float AlertTakeSeconds = 3f;

        /// <summary>Reach blind into a conscious character's pocket: slower than an open take (examine.md §5).</summary>
        public const float BlindTakeSeconds = 4.5f;

        /// <summary>
        /// True when the character is dead or unconscious: quick to take from, every slot reads plainly.
        /// </summary>
        public static bool IsLootable(HumanInventory inventory)
        {
            if (inventory == null)
            {
                return false;
            }

            HumanHealthController health = inventory.GetComponent<HumanHealthController>();
            if (health == null)
            {
                health = inventory.GetComponentInParent<HumanHealthController>();
            }

            if (health == null)
            {
                return false;
            }

            HealthSnapshot snapshot = health.Snapshot;
            return snapshot.State == HealthState.Dead || !snapshot.IsConscious;
        }

        /// <summary>How long a take from <paramref name="victim"/> lasts.</summary>
        public static float TakeSeconds(bool victimIncapacitated, bool blind)
        {
            if (victimIncapacitated)
            {
                return IncapacitatedTakeSeconds;
            }

            return blind ? BlindTakeSeconds : AlertTakeSeconds;
        }

        /// <summary>
        /// True when <paramref name="takerInventory"/> is a different character than
        /// <paramref name="victimInventory"/>.
        /// </summary>
        public static bool IsOtherCharacter(HumanInventory takerInventory, HumanInventory victimInventory)
        {
            if (takerInventory == null || victimInventory == null)
            {
                return false;
            }

            return takerInventory != victimInventory
                && takerInventory.transform.root != victimInventory.transform.root;
        }
    }
}
