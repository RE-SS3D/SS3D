using SS3D.Systems.Health;

namespace SS3D.UI.MachineInterface
{
    /// <summary>
    /// Reads a patient's health state into a <see cref="HealthScanSnapshot"/>. Pure, so the scan
    /// can be checked in EditMode without a networked body.
    /// </summary>
    public static class HealthScanSnapshotBuilder
    {
        public const int OrganCount = (int)OrganType.Liver + 1;

        /// <param name="organFunction">Function percent per <see cref="OrganType"/>; null reads the organs from <paramref name="detail"/>.</param>
        public static HealthScanSnapshot Build(
            in HealthSnapshot vitals,
            in HealthDebugDetail detail,
            float[] organFunction,
            string subjectName)
        {
            var snapshot = new HealthScanSnapshot
            {
                SubjectName = subjectName ?? string.Empty,
                State = vitals.State,
                IsConscious = vitals.IsConscious,
                IsCardiacArrest = vitals.IsCardiacArrest,
                CriticalFlags = vitals.CriticalFlags,
                BloodVolumeRatio = vitals.Pools.BloodVolumeRatio,
                OxyDebt = vitals.Pools.OxyDebt,
                Toxin = vitals.Pools.ToxinConcentration,
                Shock = vitals.Shock,
                ZoneBrute = new float[HealthConstants.ZoneCount],
                ZoneBurn = new float[HealthConstants.ZoneCount],
                ZoneFlags = new HealthScanZoneFlags[HealthConstants.ZoneCount],
                OrganFunction = new float[OrganCount],
            };

            for (int i = 0; i < HealthConstants.ZoneCount; i++)
            {
                BodyZone zone = (BodyZone)i;
                ZoneDamageState state = detail.GetZone(zone);
                snapshot.ZoneBrute[i] = state.Brute;
                snapshot.ZoneBurn[i] = state.Burn;

                HealthScanZoneFlags flags = HealthScanZoneFlags.None;
                if (vitals.IsZoneBleeding(zone) || state.BleedingRate > 0f)
                {
                    flags |= HealthScanZoneFlags.Bleeding;
                }

                if (state.IsDisabled)
                {
                    flags |= HealthScanZoneFlags.Disabled;
                }

                if (state.IsSplinted)
                {
                    flags |= HealthScanZoneFlags.Splinted;
                }

                if (state.IsSevered || vitals.IsZoneSevered(zone))
                {
                    flags |= HealthScanZoneFlags.Severed;
                }

                snapshot.ZoneFlags[i] = flags;
            }

            for (int i = 0; i < OrganCount; i++)
            {
                snapshot.OrganFunction[i] = organFunction != null && i < organFunction.Length
                    ? organFunction[i]
                    : detail.GetOrgan((OrganType)i).FunctionPercent;
            }

            return snapshot;
        }
    }
}
