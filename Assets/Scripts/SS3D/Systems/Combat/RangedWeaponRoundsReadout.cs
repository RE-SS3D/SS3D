using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Systems.Combat.Interactions;
using SS3D.Systems.Examine;
using UnityEngine;

namespace SS3D.Systems.Combat
{
    /// <summary>
    /// Replicates a gun's loaded rounds (or an energy weapon's charges) to every observer for examine
    /// (examine.md §7: rounds in the loaded magazine, up close). The fire state itself stays on
    /// <see cref="RangedWeaponItemExtension"/>, which only the server and the holder track; this mirrors
    /// the one number examine needs. Added by <c>RangedPrefabSetup.Configure</c>.
    /// </summary>
    [RequireComponent(typeof(RangedWeaponItemExtension))]
    public sealed class RangedWeaponRoundsReadout : NetworkBehaviour, IExamineContentProvider
    {
        [SyncVar]
        private int _rounds = -1;

        private RangedWeaponItemExtension _weapon;

        /// <summary>Server: publish the current count. A pure client or unspawned object ignores it.</summary>
        public void ServerPublish(int rounds)
        {
            if (NetworkObject != null && NetworkObject.IsSpawned && IsServer && _rounds != rounds)
            {
                _rounds = rounds;
            }
        }

        public void AppendSections(IExaminable examinable, List<ExamineSection> sections)
        {
            if (_weapon == null && !TryGetComponent(out _weapon))
            {
                return;
            }

            // Before the first publish (fresh spawn) the gun is full.
            int rounds = _rounds >= 0 ? _rounds : _weapon.MagazineSize;
            string line = _weapon.Profile.VisualStyle == RangedVisualStyle.Flame
                ? ExamineText.Get(ExamineWeaponKeys.Fuel, ExamineWeaponKeys.FuelFallback, rounds)
                : Line(_weapon.Profile.VisualStyle != RangedVisualStyle.Ballistic, rounds);
            sections.Add(new ExamineSection(line, ExamineBand.Close));
        }

        public static string Line(bool energy, int rounds)
        {
            if (rounds <= 0)
            {
                return energy
                    ? ExamineText.Get(ExamineWeaponKeys.ChargeEmpty, ExamineWeaponKeys.ChargeEmptyFallback)
                    : ExamineText.Get(ExamineWeaponKeys.RoundsEmpty, ExamineWeaponKeys.RoundsEmptyFallback);
            }

            return energy
                ? ExamineText.Get(ExamineWeaponKeys.Charge, ExamineWeaponKeys.ChargeFallback, rounds)
                : ExamineText.Get(ExamineWeaponKeys.Rounds, ExamineWeaponKeys.RoundsFallback, rounds);
        }
    }
}
