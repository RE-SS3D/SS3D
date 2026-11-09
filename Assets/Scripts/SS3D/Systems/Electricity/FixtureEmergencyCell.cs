using UnityEngine;

namespace SS3D.Systems.Electricity
{
    /// <summary>
    /// A light fixture's own small battery, the second layer under the APC's cell: it keeps the fixture on dim
    /// emergency light for a while after its APC can no longer light it (cell flat, lighting shed, APC broken or
    /// gone). It charges while the fixture runs on mains and only arms once it has, so a fixture that never had
    /// power (round start, Map Editor) stays dark.
    /// </summary>
    public sealed class FixtureEmergencyCell
    {
        private readonly float _runSeconds;
        private readonly float _rechargeSeconds;

        public FixtureEmergencyCell(float runSeconds, float rechargeSeconds)
        {
            _runSeconds = Mathf.Max(0.01f, runSeconds);
            _rechargeSeconds = Mathf.Max(0.01f, rechargeSeconds);
            Charge = 1f;
        }

        /// <summary>0..1 share of the run time left.</summary>
        public float Charge { get; private set; }

        /// <summary>True once the fixture has been lit from mains; until then the cell never takes over.</summary>
        public bool Armed { get; private set; }

        /// <summary>Whether the cell would light the fixture right now.</summary>
        public bool CanLight => Armed && Charge > 0f;

        /// <summary>The fixture is lit normally from its APC: arm the cell and top it up.</summary>
        public void OnMains(float seconds)
        {
            Armed = true;
            Charge = Mathf.Min(1f, Charge + (Mathf.Max(0f, seconds) / _rechargeSeconds));
        }

        /// <summary>The fixture is running on this cell: drain it.</summary>
        public void Drain(float seconds)
        {
            Charge = Mathf.Max(0f, Charge - (Mathf.Max(0f, seconds) / _runSeconds));
        }
    }
}
